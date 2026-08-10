# 位移同步模块化变更文档

## 目标

本文档只描述位移同步的实现变更方案，不包含代码实现。目标是在当前网络同步设计基础上，把“移动状态同步”从 `NetEntityCharacter` 和控制器中拆出来，形成独立、可组合、可扩展的 `NetTransformSync` 模块。

完成后，一个网络实体可以按组件组合能力：

```text
NetEntityIdentity
NetEntityRoleAssembler
NetTransformSync
NetInputSync
NetAnimatorSync
NetSkillStateSync
NetHitboxSync
...
```

其中本阶段只落地位移相关组件和接口边界，动画、技能、属性、判定体只预留扩展点。

## 当前问题

当前位移同步能力主要分散在以下位置：

- `NetEntityCharacter` 同时负责网络身份、Role 设置、`CharacterController` 配置、快照捕获、快照应用和插值。
- `LocalController` 同时负责输入采集、预测帧缓存、预测模拟、权威修正和回放。
- `AuthorityController` 同时负责输入队列、服务端模拟和快照输出。
- `RemoteController` 依赖 `NetEntityCharacter` 做快照插值应用，但 `Init` 当前是私有方法，不利于外部装配。
- `NetPlayerSnapshot` / `Player_Snapshot` 只包含位置和旋转，缺少速度、移动状态等可用于动画和插值质量的信息。
- `IEntitySnapshot` 直接包含 `Position`、`Rotation`，导致所有实体快照接口天然绑定 Transform，不利于未来技能、属性、装备等非位移快照。
- `SetRole`、`Awake`、`Subscribe` 等初始化顺序不稳定，Role 装配不可控。

本阶段的核心改进是：把位移同步收敛到 `NetTransformSync`，把身份收敛到 `NetEntityIdentity`，把按 Role 启用组件的逻辑收敛到 `NetEntityRoleAssembler`。

## 方案选择

### 采用方案：强类型位移快照 + 模块化组件

本阶段采用强类型 `NetTransformSnapshot`，不立即引入 `bytes payload` 模块化协议。

原因：

- 当前只实现位移同步，强类型结构更容易调试。
- protobuf 生成链路已经存在，继续扩展 `net_sync.proto` 成本较低。
- 后续技能、属性、动画模块增加后，再考虑 `Entity_Snapshot` 聚合或 `Entity_Module_Snapshot` payload。

推荐过渡结构：

```text
NetTransformSnapshot
- entityId
- snapshotTick
- lastProcessedInputTick
- position
- rotation
- velocity
- movementState
```

其中：

- `lastProcessedInputTick` 只对本地预测玩家有意义，远端副本可以忽略。
- `velocity` 用于远端插值、动画 Speed 推导、瞬移检测和误差分析。
- `movementState` 用于客户端动画、落地状态修正和后续状态机同步。

### 暂不采用方案：完全通用 INetSyncComponent payload

不建议第一阶段直接做通用 `NetSnapshotWriter/Reader` 和二进制 payload。

原因：

- 当前模块数量还少，过早通用化会引入协议调试成本。
- 位移同步有预测、回放、插值等特殊逻辑，和属性同步、技能事件并不完全同构。
- 可以先统一“组件生命周期和 Role 配置”，协议层稍后再统一。

## 目标架构

```text
Entity Root
  NetEntityIdentity
  NetEntityRoleAssembler
  EntityCharacter
  NetTransformSync
  NetInputSync
  Local/Authority/Remote Driver
  Hurtbox/Hitbox 子节点
  Visual/Mesh 子节点
```

位移同步的数据流：

```text
输入命令
  -> Driver 写入 EntityCharacter
  -> EntityCharacter.Simulate(tickDeltaTime)
  -> NetTransformSync.CaptureSnapshot
  -> protobuf / 网络层发送
  -> NetTransformSync.ApplyAuthoritySnapshot 或 AddRemoteSnapshot
```

组件职责：

| 组件 | 职责 |
| --- | --- |
| `NetEntityIdentity` | 保存 `EntityId`、`Role`，不处理位移 |
| `NetEntityRoleAssembler` | 根据 Role 配置组件，不处理快照细节 |
| `NetTransformSync` | 捕获、应用、插值、位移误差计算、移动碰撞配置 |
| `LocalController` / `ClientPredictDriver` | 本地输入预测、保存输入历史、回放未确认输入 |
| `AuthorityController` / `ServerAuthorityDriver` | 服务端按 Tick 消费输入并输出权威快照 |
| `RemoteController` / `RemoteInterpolationDriver` | 缓冲远端快照并驱动插值 |
| `NetSyncUtils` | 协议对象和运行时 snapshot 互转 |

## 文件变更清单

### 新增文档

| 文件 | 变更 |
| --- | --- |
| `Docs/NetworkTransformSyncChangePlan.md` | 本文档 |

### 新增运行时脚本

| 文件 | 目的 |
| --- | --- |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Component/INetSyncComponent.cs` | 定义同步组件最小 Role 生命周期接口 |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Component/NetEntityIdentity.cs` | 替代 `NetEntityCharacter` 的身份职责 |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Component/NetEntityRoleAssembler.cs` | 根据 Role 分发配置同步组件和 Driver |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Component/NetTransformSync.cs` | 位移同步组件，承接 Capture、Apply、Interpolate |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Snapshot/NetTransformSnapshot.cs` | 位移快照数据结构 |

如果希望保持依赖 `EntityCharacter` 的代码靠近实体系统，也可以放在：

```text
Assets/Scripts/GamePlay/EntitySystem/Network/
```

建议本阶段采用 `MultiPlaySystem/Component`，因为位移同步本质属于网络同步模块；对 `EntityCharacter` 的桥接通过序列化引用或 `RequireComponent` 表达。

### 修改运行时脚本

| 文件 | 需要调整的职责 |
| --- | --- |
| `Assets/Scripts/GamePlay/EntitySystem/Character/NetEntityCharacter.cs` | 拆分职责。最终删除或改名为兼容包装，不再负责快照和 Role 装配 |
| `Assets/Scripts/GamePlay/EntitySystem/Controller/LocalController.cs` | 从 `NetEntityCharacter` 切换到 `NetEntityIdentity` + `NetTransformSync`；预测帧使用 `NetTransformSnapshot` |
| `Assets/Scripts/GamePlay/EntitySystem/Controller/AuthorityController.cs` | 从 `NetEntityCharacter.CaptureSnapshot` 切到 `NetTransformSync.CaptureSnapshot` |
| `Assets/Scripts/GamePlay/EntitySystem/Controller/RemoteController.cs` | 公开配置入口，依赖 `NetTransformSync` 做 AddSnapshot/Interpolate |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Snapshot/NetPlayerSnapshot.cs` | 改为兼容旧协议的过渡结构，或替换为 `NetTransformSnapshot` |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Snapshot/NetEntitySnapshot.cs` | 移除 Transform 假设，或保留为旧兼容结构 |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Interface/IEntitySnapshot.cs` | 从通用快照接口中移除 `Position`、`Rotation`，只保留 `EntityId`、`SnapshotTick` |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Interface/IPlayerSnapshot.cs` | 评估是否改名为 `IConfirmedInputSnapshot`，避免和玩家强绑定 |
| `Assets/Scripts/Utils/NetSyncUtils.cs` | 增加 `NetTransformSnapshot` 和 protobuf message 的互转 |
| `Assets/Scripts/GamePlay/MultiPlaySystem/Config/SyncConfig.cs` | 增加位移同步阈值、插值缓冲、瞬移阈值、误差修正阈值等配置 |
| `Assets/Scripts/GamePlay/EntitySystem/Character/EntityCharacter.cs` | 不直接依赖网络组件，只保留 `tickDrive` 和 `Simulate`；必要时开放只读移动状态 |
| `Assets/Scripts/GamePlay/EntitySystem/EntityContext.cs` | 暴露移动状态读取能力，例如 `locomotionSpeed`、当前状态 key、落地状态 |

### 修改协议文件

| 文件 | 需要调整的职责 |
| --- | --- |
| `Assets/Scripts/GamePlay/Protocol/net_sync.proto` | 新增或替换 `Transform_Snapshot` / `Transform_State` message |
| `Assets/Scripts/GamePlay/Protocol/Generated/NetSync.cs` | 由 protoc 重新生成，不手工修改 |

建议协议过渡方式：

```text
第一步：保留 Player_Snapshot，新增 velocity/movementState 字段。
第二步：新增 Transform_Snapshot，并让 World_Snapshot repeated Transform_Snapshot。
第三步：删除或停止使用 Player_Snapshot。
```

如果希望少改网络消息处理，第一步可以先扩展 `Player_Snapshot`；如果希望架构更干净，直接新增 `Transform_Snapshot`。

### 需要确认或恢复的文件

当前工作区中以下文件显示为删除状态：

```text
Assets/Scripts/GamePlay/MultiPlaySystem/MultiPlayManager.cs
Assets/Scripts/GamePlay/MultiPlaySystem/Simulator/ClientSimulator.cs
Assets/Scripts/GamePlay/MultiPlaySystem/Simulator/ServerSimulator.cs
```

这些文件若仍是网络同步入口，则位移同步落地前需要先确认：

- 是恢复旧 Simulator 后改造；
- 还是用新的 Manager/Driver 架构替代；
- 或者当前删除是一次进行中的重构，变更计划应接入新的入口。

本文档后续实施步骤默认存在等价的客户端/服务端 Tick 驱动入口。

## NetTransformSync 职责边界

`NetTransformSync` 只负责位移状态，不负责输入、技能、动画或伤害。

应包含：

- `CaptureSnapshot(snapshotTick, lastProcessedInputTick)`
- `ApplyAuthoritySnapshot(snapshot)`
- `AddRemoteSnapshot(snapshot)`
- `UpdateInterpolation(deltaTime)`
- `ApplyInterpolatedSnapshot(from, to, t)`
- `ConfigureRole(role)`
- `SetMovementColliderEnabled(enabled)`
- `CalculateVelocity(previous, current, tickDeltaTime)`
- `CalculateError(authority, predicted)`

不应包含：

- 读取键盘或手柄输入。
- 发送网络消息。
- 管理技能释放。
- 设置 Animator Trigger。
- 计算伤害或命中。
- 创建玩家实体。

这样后续 `NetSkillStateSync`、`NetAnimatorSync`、`NetHitboxSync` 可以和它并列存在，而不是依赖它内部的特殊分支。

## Role 下的位移行为

| Role | `NetTransformSync` 行为 |
| --- | --- |
| `Authority` | 启用 `CharacterController`；服务端模拟后捕获权威快照；不做插值 |
| `Predict` | 启用 `CharacterController`；本地预测后捕获预测帧；收到权威快照后回滚和重放 |
| `Replica` | 禁用移动用 `CharacterController`；缓存权威快照；每帧插值应用 Transform；保留 hurtbox/target Collider |
| `LocalPlay` | 启用 `CharacterController`；不走网络快照；可选择 Tick 驱动以保持和联机一致 |

注意：Replica 禁用移动碰撞，不等于禁用攻击判定 Collider。攻击判定由后续 `NetHitboxSync` 或战斗模块管理。

## 本地预测与回放方案

本地预测仍采用当前思路：

```text
inputTick
  -> 读取 InputState
  -> NetDriverInput.ApplyTo(EntityCharacter)
  -> EntityCharacter.Simulate(tickDeltaTime)
  -> NetTransformSync.CaptureSnapshot()
  -> 保存 PredictFrame
```

收到权威快照：

```text
authoritySnapshot.lastProcessedInputTick
  -> 丢弃已确认输入
  -> ApplyAuthoritySnapshot
  -> 从 lastProcessedInputTick + 1 回放到 currentInputTick
  -> 比较回放后状态和原预测状态
  -> 触发视觉平滑或误差纠正
```

预测帧建议从：

```text
PredictFrame
- inputTick
- InputState
- NetPlayerSnapshot
```

调整为：

```text
PredictFrame
- inputTick
- InputState
- NetTransformSnapshot
```

预测缓冲长度使用 `SyncConfig.maxBufferedInputs`。

## 远端插值方案

远端副本使用 `SnapshotBuffer<NetTransformSnapshot>`。

流程：

```text
收到 TransformSnapshot
  -> 校验 entityId
  -> 加入 SnapshotBuffer
  -> 首帧直接应用
  -> 后续以 latestTick - interpolationDelayTicks 为目标渲染时间
  -> 在 from/to 之间插值 position 和 rotation
  -> 推导 velocity，供动画模块读取
```

插值策略：

- position 使用 `Vector3.Lerp`。
- rotation 使用 `Quaternion.Slerp`。
- velocity 不直接插值时，可由两帧位置差除以 tick 时间得到。
- 如果快照间距过大或距离超过瞬移阈值，直接 snap 到最新状态。

`NetTransformSync` 应暴露当前插值状态给动画或表现层读取，例如：

```text
CurrentRenderVelocity
CurrentMovementState
CurrentGroundedState
```

这里是只读状态，不要求动画模块反向影响位移模块。

## 权威服务端方案

服务端只运行 gameplay 状态机和物理/判定逻辑，不运行 Animator。

每个 simulation tick：

```text
ServerAuthorityDriver
  -> 消费一帧或多帧输入
  -> NetDriverInput.ApplyTo(EntityCharacter)
  -> EntityCharacter.Simulate(tickDeltaTime)
  -> NetTransformSync.CaptureSnapshot(simulationTick, lastProcessedInputTick)
```

每个 snapshot tick：

```text
收集所有 Authority 实体的 NetTransformSnapshot
  -> 转成 protobuf message
  -> 广播 World_Snapshot 或 Transform_World_Snapshot
```

本阶段不把技能、伤害或动画事件放进位移快照。

## 单机模式方案

单机模式推荐复用同一套组件，但关闭网络收发：

```text
NetEntityRole.LocalPlay
  -> NetEntityRoleAssembler 配置 Local 输入
  -> EntityCharacter 本地模拟
  -> NetTransformSync 不发送快照
  -> NetAnimatorSync 从本地状态推导动画
```

是否使用 Tick 驱动由项目选择：

- 若追求单机和联机行为一致，`LocalPlay` 也使用 `TickSystem` 驱动。
- 若追求单机开发便利，可以保留 `Update` 驱动，但技能和移动预测逻辑会更难复用。

建议位移同步阶段先支持 `LocalPlay` 跳过网络快照，但保留 Tick 驱动入口。

## 高模块化扩展方案

### 统一身份，不统一业务

所有同步模块共享 `NetEntityIdentity`，但每个模块只处理自己的数据：

```text
NetTransformSync -> 位置、旋转、速度、移动状态
NetSkillStateSync -> 技能阶段、预测技能命令、服务端技能事件
NetAttributeSync -> HP、能量、弹药
NetAnimatorSync -> 客户端动画表现
NetHitboxSync -> 判定体配置和窗口
```

模块之间可以读取必要的只读状态，但不能互相接管生命周期。

### Role 分发由 Assembler 统一处理

`NetEntityRoleAssembler` 负责：

- 找到所有 `INetSyncComponent`。
- 按 Role 调用 `ConfigureRole`。
- 添加或配置 Driver。
- 保证幂等。

每个同步模块内部只关心自己在当前 Role 下启用哪些路径。

### Driver 只编排，不存业务状态

Driver 可以编排 Tick 流程，但业务状态由组件持有：

```text
ClientPredictDriver
  -> 读输入
  -> 调 EntityCharacter.Simulate
  -> 调 NetTransformSync.CaptureSnapshot
```

不要让 Driver 同时保存所有模块状态。后续技能预测加入时，Driver 可以按顺序调用：

```text
NetInputSync
NetSkillStateSync
EntityCharacter
NetTransformSync
```

而不是把技能预测逻辑塞进位移同步。

### Snapshot 接口按模块拆分

`IEntitySnapshot` 不应强制包含 `Position`、`Rotation`。建议：

```text
IEntitySnapshot
- EntityId
- SnapshotTick

IInputConfirmSnapshot
- LastProcessedInputTick

NetTransformSnapshot : IEntitySnapshot, IInputConfirmSnapshot
```

未来：

```text
NetSkillSnapshot : IEntitySnapshot
NetAttributeSnapshot : IEntitySnapshot
NetAnimatorEvent : IEntityEvent
```

### 协议先强类型，后模块聚合

阶段一：

```text
Transform_Snapshot
World_Snapshot.repeated transform_snapshots
```

阶段二：

```text
Entity_Snapshot
  optional Transform_State
  optional Skill_State
  optional Attribute_State
```

阶段三：

```text
Entity_Module_Snapshot
  moduleId
  bytes payload
```

不要在位移同步阶段一次性完成所有协议泛化。

## 实施步骤

### 步骤 1：定义边界

1. 新增 `NetEntityIdentity`。
2. 新增 `INetSyncComponent`。
3. 新增 `NetEntityRoleAssembler`。
4. 保留 `NetEntityCharacter` 作为过渡组件，或直接规划删除。

验收标准：

- `EntityId` 和 `Role` 不再依赖位移同步组件。
- Role 设置会分发给同步组件。

### 步骤 2：抽出位移快照

1. 新增 `NetTransformSnapshot`。
2. 调整 `IEntitySnapshot`，移除 Transform 假设。
3. `NetTransformSync` 接管 `CaptureSnapshot`、`ApplySnapshot`、`ApplyInterpolatedSnapshot`。

验收标准：

- 捕获和应用位移快照不再经过 `NetEntityCharacter`。
- 远端插值只依赖 `NetTransformSync` 和 `SnapshotBuffer<NetTransformSnapshot>`。

### 步骤 3：改造三类位移驱动

1. `AuthorityController` 依赖 `NetTransformSync` 输出权威快照。
2. `LocalController` 预测帧缓存 `NetTransformSnapshot`。
3. `RemoteController` 公开初始化入口，依赖 `NetTransformSync` 添加和插值快照。

验收标准：

- Authority、Predict、Replica 三条路径都只通过 `NetTransformSync` 读写 Transform。
- Driver 只负责编排 Tick，不直接操作 Transform。

### 步骤 4：协议和转换层

1. 修改 `net_sync.proto`。
2. 重新生成 `Generated/NetSync.cs`。
3. 修改 `NetSyncUtils` 的转换函数。

验收标准：

- 位移快照协议包含 position、rotation、velocity、movementState、lastProcessedInputTick。
- 不手工修改生成文件。

### 步骤 5：Prefab 和场景装配

1. 玩家 prefab 增加 `NetEntityIdentity`、`NetEntityRoleAssembler`、`NetTransformSync`。
2. 移除或停用旧 `NetEntityCharacter` 的位移职责。
3. Replica 配置禁用 `CharacterController`，保留攻击判定 Collider。

验收标准：

- 外部只需设置 `Identity.Init(entityId, role)`。
- Role 切换不会重复添加 Collider 或重复订阅事件。

### 步骤 6：验证

1. 单机 LocalPlay 能正常移动。
2. 服务端 Authority 能按 Tick 模拟并广播位移快照。
3. 客户端 Predict 能本地预测、收到权威快照后回放。
4. Remote Replica 能插值移动。
5. Replica 关闭移动碰撞后，hitbox/hurtbox 仍可用于攻击查询。
6. Unity 编译无错误，相关开发场景可运行。

## 风险与处理

| 风险 | 处理 |
| --- | --- |
| 拆分后引用链断裂 | 先保留 `NetEntityCharacter` 作为兼容层，逐步迁移调用方 |
| 协议改动影响现有网络测试 | 先扩展旧 `Player_Snapshot`，再切换到 `Transform_Snapshot` |
| 本地预测回放不稳定 | 固定使用 TickDeltaTime，禁止回放中使用 `Time.deltaTime` |
| Replica Collider 配置不清 | 明确 Movement Collider 和 Hurtbox Collider 分层 |
| 过早抽象导致复杂 | 先强类型位移模块，等技能/属性模块落地后再协议泛化 |
| 当前 Simulator 文件被删除 | 先确认网络入口重构方向，再接入位移同步调用点 |

## 不在本阶段处理

- 技能预测实现。
- 伤害和命中事件同步。
- Animator trigger 同步。
- 属性、装备、Buff 同步。
- 通用二进制 payload 协议。
- 完整 rollback world state。

本阶段只保证位移同步成为独立模块，并为后续同步模块留出清晰边界。
