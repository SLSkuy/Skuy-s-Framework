# 网络同步模块化设计改进草案

## 背景

当前网络同步已经具备以下基础能力：

- `TickSystem` 驱动固定频率模拟。
- `SyncConfig` 配置模拟 Tick、快照频率、插值延迟和输入缓冲长度。
- `SnapshotBuffer<T>` 按服务端 Tick 缓存快照，并为远端插值提供采样。
- `NetPlayerSnapshot` / `NetEntitySnapshot` 表达实体位置、旋转和玩家输入确认 Tick。
- `AuthorityController`、`LocalController`、`RemoteController` 分别承载服务端权威模拟、本地预测和远端插值。
- `EntityCharacter` 聚合移动状态机、`EntityMotor` 和 `EntityAnimator`，通过 `Simulate(deltaTime)` 支持外部 Tick 驱动。

现有方向是：玩家对象实例化后，外部只设置网络角色 `Role`，框架自动完成组件装配，并把网络层和 `EntityCharacter` 桥接起来。这个方向是合理的，但需要避免把所有同步功能都塞进一个 `NetEntityCharacter` 里。

本设计建议采用“网络身份 + 角色装配器 + 多个同步组件”的模式。一个组件只同步一类信息，例如移动、动画、技能、属性、装备等。这样当前移动同步可以独立落地，后续技能状态、战斗事件、Buff、外观等同步也能按组件扩展。

## 设计目标

1. 外部创建网络实体时，只需要设置 `EntityId` 和 `Role`。
2. 每类同步信息由独立组件负责采集、应用、插值、回滚或事件分发。
3. 服务端权威、本地预测、远端副本使用同一套实体能力，但启用不同驱动路径。
4. 动画同步和移动同步解耦，避免 Animator 参数污染预测和回放逻辑。
5. 协议层支持分模块扩展，避免 `Player_Snapshot` 无限膨胀。
6. 单机、联机、本地预测、服务端模拟可以共享 `EntityCharacter` 的核心行为。

## 已确认约束

1. 一个网络实体可以同时拥有多个同步模块，例如移动、动画、技能、属性、装备、判定体等模块挂在同一实体根节点下。
2. 技能系统需要支持客户端预测。技能释放的本地反馈可以立即播放，但伤害、命中、死亡等结果事件必须由服务端权威同步。
3. 单机也要走同一套 gameplay 组件。网络收发层可关闭，但输入、移动、技能、动画桥接应尽量复用。
4. 后续不会依赖 Animator root motion 作为权威位移来源。当前 root motion 只作为测试或表现能力保留。
5. 服务端不运行 Animator，只运行 gameplay 状态机和权威判定逻辑。动画由客户端根据状态、快照和事件自行处理。
6. 远端玩家需要参与攻击判定。Replica 可以禁用 `CharacterController`，但需要保留或配置专用 hitbox/hurtbox Collider。

## 推荐组件边界

### NetEntityIdentity

只表达网络身份，不承担快照和装配逻辑。

职责：

- 保存 `EntityId`。
- 保存 `NetEntityRole`。
- 提供 `IsAuthority`、`IsPredictingOwner`、`IsReplica`、`IsLocalPlay` 等便捷判断。
- 在 `Init(entityId, role)` 时通知装配器或同步组件角色已变化。

建议替代当前 `NetEntityCharacter` 的身份部分。

### NetEntityRoleAssembler

角色装配器，负责根据 `Role` 启用或添加对应组件。

职责：

- 收集同一 GameObject 上的同步组件。
- 根据 `Role` 调用同步组件的 `ConfigureRole(role)`。
- 为玩家实体添加本地输入 Provider、预测 Driver、权威 Driver 或远端插值 Driver。
- 配置 `CharacterController`、运动碰撞体、攻击判定体、视觉平滑器等角色相关运行时状态。
- 保证重复设置 Role 是幂等的。

装配器不应该假设一个实体只有一个同步组件。推荐启动时扫描：

```csharp
_syncComponents = GetComponentsInChildren<INetSyncComponent>(includeInactive: true);
```

然后按模块职责分发 Role，而不是在装配器里直接写每个模块的具体业务逻辑。

装配器可以是 MonoBehaviour，挂在网络实体根节点。外部调用入口可以保持简单：

```csharp
identity.Init(entityId, NetEntityRole.Predict);
assembler.ApplyRole(identity.Role);
```

后续如果希望进一步自动化，可以让 `NetEntityIdentity.Init` 内部触发 `NetEntityRoleAssembler.ApplyRole`。

### INetSyncComponent

所有同步组件的公共接口。组件只负责自己的状态，不负责实体生成和网络收发。

```csharp
public interface INetSyncComponent
{
    ushort ModuleId { get; }
    void ConfigureRole(NetEntityRole role);
    void Capture(uint snapshotTick, NetSnapshotWriter writer);
    void Apply(uint snapshotTick, NetSnapshotReader reader);
}
```

实际实现时可以先不用抽象 `NetSnapshotWriter/Reader`，直接通过具体 snapshot struct 过渡。等同步模块增多后，再引入模块化 payload。

### NetTransformSync

同步实体移动状态。它是当前 `NetEntityCharacter` 快照能力的主要归宿。

职责：

- 捕获位置、旋转、速度、可选移动状态。
- 在服务端权威实体上生成移动快照。
- 在预测实体上应用权威修正，并支持重放后的视觉平滑。
- 在远端副本上用 `SnapshotBuffer<NetTransformSnapshot>` 做插值。
- 控制远端 `CharacterController` 是否禁用，是否使用简化碰撞体。

建议移动快照包含：

```text
entityId
snapshotTick
lastProcessedInputTick
position
rotation
velocity
movementState
```

其中 `movementState` 可选，用于动画和状态机纠偏。例如 idle、walk、run、sprint、airborne。

### NetInputSync

同步玩家输入命令。它不是状态快照，而是客户端到服务端的命令流。

职责：

- 从 `IInputStateProvider` 读取输入。
- 给输入分配 `inputTick`。
- 上传输入命令。
- 服务端按 Tick 排队消费。
- 为本地预测保存输入历史，供回滚重放。

当前 `NetDriverInput.ApplyTo` 可以继续作为输入到 `EntityCharacter` 的桥接工具，但建议把输入采集、发送、历史缓存放到 `NetInputSync` 或 `ClientPredictDriver` 中。

输入同步适合包含：

```text
entityId
inputTick
moveInput
aimInput
held buttons
pressed edge buttons
clientViewYaw / aimTarget
```

持续型输入和边沿型输入要明确区分。边沿型输入必须按 Tick 处理，例如跳跃、技能释放、模式切换。

### NetAnimatorSync

同步动画表现，不直接决定 gameplay 权威状态。

职责：

- 根据本地 `EntityContext` 或移动快照驱动 Animator 参数。
- 同步必须一致的动画事件，例如攻击、换弹、受击、技能起手。
- 对远端副本使用插值后的速度或快照中的 `movementState` 计算动画参数。
- 对 trigger 类动画使用带 Tick 的事件同步，而不是只同步 Animator trigger 当前值。
- 只在客户端或单机表现层工作。服务端不运行 Animator，也不依赖 Animator 状态做权威判断。

动画数据建议分三类：

| 类型 | 示例 | 同步方式 |
| --- | --- | --- |
| 连续参数 | Speed、AimYaw、AimPitch | 可由移动/瞄准状态推导，必要时低频快照 |
| 离散状态 | LocomotionState、WeaponMode、IsAiming | 放入状态快照或状态模块 |
| 事件触发 | Jump、Attack、Reload、SkillCast、HitReact | 可靠或带序号的事件流，附带触发 Tick |

移动类动画不要每帧同步 Animator 参数。当前 `EntityAnimator` 的 `Speed` 可以继续从 `EntityContext.locomotionSpeed` 推导；远端实体可以由 `NetTransformSync` 提供的速度或位移差得到目标速度。

### NetSkillStateSync

后续技能同步组件。它应独立于移动同步和动画同步。

职责：

- 同步技能释放请求、技能阶段、冷却、蓄力、引导、命中确认等状态。
- 支持客户端预测。客户端可以先进入技能起手、播放动画和特效，并缓存预测帧。
- 服务端验证技能命令，并广播权威技能事件。
- 伤害、命中、死亡、打断等结果必须以服务端事件为准。
- 单机模式复用同一套技能执行路径，只跳过网络发送和服务端回包等待。
- 技能事件驱动动画、特效、音效，但动画/特效不是技能权威状态本身。

技能同步建议拆成两条流：

```text
SkillCommand: client -> server
SkillEvent / SkillStateSnapshot: server -> clients
```

例如：

```text
SkillCommand
- entityId
- inputTick
- skillId
- targetEntityId
- targetPosition
- aimDirection

SkillEvent
- entityId
- eventTick
- skillId
- phase
- eventSequence
- targetEntityId
- hitResult
```

技能预测建议分层：

| 层级 | 是否可预测 | 说明 |
| --- | --- | --- |
| 起手表现 | 是 | 动画、特效、音效可以立即播放 |
| 位移意图 | 可选 | 冲刺、突进等应转成 gameplay movement command，而不是 root motion |
| 冷却 UI | 是 | 可先显示预测冷却，收到服务端结果后修正 |
| 命中判定 | 否 | 以服务端或单机权威世界判定为准 |
| 伤害结果 | 否 | 由服务端 `SkillEvent` / `DamageEvent` 同步 |

### NetAttributeSync

同步低频、权威属性。

职责：

- 生命值、护盾、能量、弹药、等级、阵营等。
- 服务端权威写入，客户端只读展示。
- 支持脏标记，变化时发送，避免每个快照都带。

属性数据通常不需要预测，除非是本地玩家 UI 反馈。预测 UI 和权威属性要能校正。

### NetEquipmentSync

同步装备、武器和外观挂点。

职责：

- 当前武器、皮肤、挂件、模型变体。
- 角色生成时全量同步。
- 运行中变化时可靠事件或低频状态同步。

装备变化会影响动画控制器、技能表和输入解释，因此应在装配完成后明确通知相关组件。

### NetEventSync

同步短生命周期事件。

职责：

- 命中、受击、死亡、复活、特效、音效、交互反馈。
- 处理事件序号，避免重复播放。
- 支持可靠事件和可丢弃事件。

事件不应该塞进状态快照里长期保存，除非它需要新加入玩家恢复现场状态。

### NetHitboxSync

同步或装配攻击判定体。它负责战斗判定所需的 Collider 状态，不负责移动模拟。

职责：

- 配置 hurtbox、hitbox、lock-on target、弱点等判定体。
- 根据 `Role` 决定 Collider 是否参与查询、触发器或物理碰撞。
- 支持技能期间临时开启 hitbox，例如挥砍窗口、爆炸范围、投射物碰撞。
- 为服务端权威判定提供稳定的碰撞数据。
- 为客户端表现和辅助瞄准提供可查询的远端判定体。

推荐把移动碰撞和攻击判定拆开：

| 类型 | 示例 | 作用 | Replica 配置 |
| --- | --- | --- | --- |
| Movement Collider | `CharacterController` | 移动、阻挡、地面检测 | 通常禁用，避免和快照插值抢位移 |
| Hurtbox Collider | 身体、头部、弱点 | 被攻击判定、锁定、射线检测 | 保持启用，通常设为 Trigger 或专用 Layer |
| Hitbox Collider | 武器挥砍、技能范围 | 主动攻击判定 | 按技能窗口启用 |

远端 Replica 禁用 `CharacterController` 不等于禁用所有 Collider。攻击判定所需 Collider 应由 `NetHitboxSync` 或技能模块明确管理。

## 同步信息划分

| 信息类别 | 示例 | 权威来源 | 传输方向 | 推荐模块 | 可靠性 |
| --- | --- | --- | --- | --- | --- |
| 输入命令 | 移动、瞄准、跳跃、技能键 | 拥有者客户端产生，服务端验证 | Client -> Server | NetInputSync | 可不可靠但要按 Tick 去重，关键命令可冗余发送 |
| 移动状态 | 位置、旋转、速度、落地状态 | Server | Server -> Clients | NetTransformSync | 快照流，可丢包，持续覆盖 |
| 预测确认 | lastProcessedInputTick | Server | Server -> Owner | NetTransformSync / PredictionDriver | 快照流 |
| 动画连续参数 | Speed、AimYaw | 本地推导或 Server 状态 | Server -> Clients 或本地推导 | NetAnimatorSync | 低频或不传 |
| 动画事件 | Attack、Reload、HitReact | Server 或预测后确认 | Server -> Clients | NetAnimatorSync / NetEventSync | 可靠或带序号冗余 |
| 技能命令 | skillId、目标、方向 | Owner Client | Client -> Server | NetSkillStateSync | 可靠或冗余输入流 |
| 技能状态 | 起手、释放、冷却、命中 | Server | Server -> Clients | NetSkillStateSync | 状态可快照，事件需可靠 |
| 攻击判定 | hitbox、hurtbox、锁定点 | Server 配置，客户端可查询 | Spawn/State -> Clients | NetHitboxSync | 配置可靠，窗口事件可靠 |
| 属性 | HP、MP、弹药、护盾 | Server | Server -> Clients | NetAttributeSync | 脏同步，通常可靠 |
| 装备外观 | weaponId、skinId | Server | Server -> Clients | NetEquipmentSync | 可靠 |
| 生命周期 | Spawn、Despawn、死亡、复活 | Server | Server -> Clients | EntitySpawner / NetEventSync | 可靠 |

## 快照与事件的取舍

使用快照的场景：

- 状态持续存在。
- 新快照可以覆盖旧快照。
- 丢一两包不影响最终一致性。
- 适合插值或校正。

例如位置、旋转、速度、移动状态、技能当前阶段。

使用事件的场景：

- 状态只发生一次。
- 不能因为下一个快照覆盖而丢失表现。
- 需要严格去重。

例如开火、命中、换弹完成、技能释放、死亡、复活。

使用命令的场景：

- 客户端表达“我想做什么”。
- 服务端需要验证。
- 本地可以预测，但最终以服务端结果为准。

例如移动输入、跳跃请求、技能释放请求、交互请求。

## 角色行为矩阵

| Role | 模拟来源 | 输入来源 | 移动同步 | 动画策略 | 物理配置 |
| --- | --- | --- | --- | --- | --- |
| Authority | 服务端 Tick | 收到的输入队列 | Capture 权威快照 | 不运行 Animator，只运行 gameplay 状态 | `CharacterController` 启用，判定 Collider 启用 |
| Predict | 本地 Tick 预测 + 服务端校正 | 本地输入 | Capture 预测帧，Apply 权威修正，Replay 未确认输入 | 本地立即响应，校正时平滑 | `CharacterController` 启用 |
| Replica | 快照插值 | 无 | Buffer + Interpolate | 根据插值速度和事件驱动 | 禁用 `CharacterController`，保留 hurtbox/target Collider |
| LocalPlay | 本地 Update 或 Tick | 本地输入 | 不走网络 | 本地驱动 | `CharacterController` 启用 |

`Role` 切换必须是幂等操作。重复调用 `ApplyRole(NetEntityRole.Replica)` 不应该重复添加 Collider、重复订阅事件或重复创建 Driver。

## 推荐目录结构

可以逐步整理为：

```text
Assets/Scripts/GamePlay/MultiPlaySystem
  Config/
  Snapshot/
  Driver/
    ClientPredictDriver.cs
    ServerAuthorityDriver.cs
    RemoteInterpolationDriver.cs
  Component/
    INetSyncComponent.cs
    NetEntityIdentity.cs
    NetEntityRoleAssembler.cs
    NetTransformSync.cs
    NetInputSync.cs
    NetAnimatorSync.cs
    NetSkillStateSync.cs
    NetAttributeSync.cs
    NetHitboxSync.cs
  ProtocolAdapter/
    NetSyncUtils.cs
```

如果希望保持实体相关代码集中，也可以把和 `EntityCharacter` 强绑定的组件放在：

```text
Assets/Scripts/GamePlay/EntitySystem/Network/
```

两种都可以。建议规则是：

- 通用网络同步能力放 `MultiPlaySystem`。
- 依赖 `EntityCharacter`、`EntityContext`、`EntityMotor` 的桥接组件放 `EntitySystem`。

## 协议组织建议

当前 `Player_Snapshot` 只包含移动状态。后续模块增多后，不建议把所有字段都追加到一个巨大 message 中。

可选方案一：按实体聚合，各模块可选。

```proto
message Entity_Snapshot {
  uint32 entityId = 1;
  uint32 snapshotTick = 2;
  optional Transform_State transform = 3;
  optional Animator_State animator = 4;
  optional Skill_State skill = 5;
  optional Attribute_State attribute = 6;
  optional Hitbox_State hitbox = 7;
}
```

优点是结构清晰。缺点是每加模块都要改协议。

可选方案二：模块化 payload。

```proto
message Entity_Module_Snapshot {
  uint32 entityId = 1;
  uint32 snapshotTick = 2;
  uint32 moduleId = 3;
  bytes payload = 4;
}
```

优点是扩展性强。缺点是调试不如强类型 message 直观。

项目早期建议使用方案一，等同步模块稳定后再考虑方案二。现在移动、动画、技能、判定体几类需求还在定型，用强类型 message 更容易调试。

## 当前代码的演进步骤

### 第一阶段：修正身份与移动同步边界

1. 将当前 `NetEntityCharacter` 拆分或改名：
   - 身份部分迁移到 `NetEntityIdentity`。
   - 快照部分迁移到 `NetTransformSync`。
   - 角色装配部分迁移到 `NetEntityRoleAssembler`。
2. 确保 `SetRole` 真正写入 `role`，并保证重复调用幂等。
3. `NetTransformSync` 接管：
   - `CaptureSnapshot`
   - `ApplySnapshot`
   - `ApplyInterpolatedSnapshot`
   - 远端 `CharacterController` 禁用和 Collider 配置
4. `AuthorityController`、`LocalController`、`RemoteController` 改为依赖 `NetTransformSync`，不直接依赖身份组件做快照。

### 第二阶段：统一 Driver 配置

1. 为三类驱动提供明确 `Configure(...)` 入口。
2. 避免在 `Awake` 中订阅还未注入的输入对象。
3. 预测缓冲由 `SyncConfig.maxBufferedInputs` 创建。
4. `LocalPlay` 不应被默认设置为 `tickDrive = true`，除非由装配器明确指定 Tick 驱动。

### 第三阶段：动画同步组件化

1. 新增 `NetAnimatorSync`。
2. 移动动画先从 `NetTransformSync` 推导速度，不单独同步每帧 Animator 参数。
3. Trigger 类动画通过带 Tick 的事件同步。
4. 技能、受击、换弹等表现事件由 `NetEventSync` 或技能模块通知动画模块。

### 第四阶段：技能状态同步

1. 新增 `NetSkillStateSync`。
2. 定义 `SkillCommand` 和 `SkillEvent`。
3. 客户端预测起手表现，服务端验证后广播权威事件。
4. 技能状态不要和移动状态耦合，但可以影响移动模块，例如定身、冲刺、击退。
5. 新增或接入 `NetHitboxSync`，把技能判定窗口、hurtbox 查询和伤害结果事件拆开处理。

## 组件协作流程

### 本地预测玩家

```text
LocalInputProvider
  -> NetInputSync 捕获 inputTick
  -> ClientPredictDriver 写入 EntityCharacter
  -> EntityCharacter.Simulate(tickDeltaTime)
  -> NetTransformSync 捕获预测帧
  -> 发送 Player_Input
```

收到服务端快照：

```text
NetTransformSync.ApplyAuthoritySnapshot
  -> 回到权威位置
  -> ClientPredictDriver Replay 未确认输入
  -> NetworkVisualSmoother 平滑视觉误差
  -> NetAnimatorSync 根据校正后状态修正动画
```

### 服务端权威玩家

```text
NetInputSync 接收输入
  -> ServerAuthorityDriver 按 Tick 消费
  -> EntityCharacter.Simulate(tickDeltaTime)
  -> NetSkillStateSync 执行权威技能逻辑
  -> NetHitboxSync 查询命中和判定体
  -> NetTransformSync Capture
  -> NetEventSync 生成命中/伤害/死亡事件
  -> WorldSnapshot 广播
```

### 远端副本玩家

```text
收到 TransformSnapshot
  -> NetTransformSync.AddSnapshot
  -> SnapshotBuffer 排序缓存
  -> UpdateInterpolation 插值位置/旋转/速度
  -> NetAnimatorSync 根据插值状态驱动动画
```

## 动画同步细节

服务端不运行 Animator。Animator 是客户端表现层，不能作为权威状态来源。服务端只产生可同步的 gameplay 状态和事件，例如移动状态、技能阶段、命中结果、死亡事件；客户端再把这些状态翻译成 Animator 参数、Trigger、Playable 或特效播放。

移动动画建议使用“状态 + 推导参数”：

- `Speed`：远端由两帧插值位置差或快照速度推导。
- `IsGrounded`：可由移动状态快照携带，避免远端本地物理判断不一致。
- `MovementState`：由服务端状态机输出，如 idle、walk、run、sprint、airborne。
- `AimYaw/AimPitch`：如果影响上半身瞄准，需要同步或从 aim 输入/方向推导。

事件动画建议使用“事件序号 + Tick”：

```text
eventSequence
eventTick
eventType
eventParam
```

远端收到后，如果事件序号已播放则丢弃；如果事件 Tick 早于当前插值时间，可以立即播放或按补偿时间播放。

Root Motion 建议：

- 网络预测角色不使用 root motion 位移。
- 动画可以播放 root motion，但移动仍由 `EntityMotor` 和服务端状态驱动。
- 技能位移使用配置数据描述，例如位移曲线、持续 Tick、速度、方向锁定规则，并转成 gameplay movement command。
- 如果未来必须使用动画曲线位移，也应先烘焙或导出为配置数据，由服务端同样模拟，而不是让 Animator 自己移动 Transform。

## 需要避免的问题

1. 一个 `NetEntityCharacter` 同时负责身份、装配、输入、移动、动画、技能。
2. 远端副本继续运行完整本地状态机，导致和快照插值抢 Transform。
3. Animator trigger 只设置本地，不带 Tick 和序号，远端丢包后无法恢复。
4. 技能表现事件塞进移动快照，导致事件被覆盖或重复播放。
5. `Role` 切换重复添加组件或重复订阅事件。
6. 本地预测重放时混入 `Time.deltaTime`，破坏确定性。
7. 协议字段无限追加到单个 `Player_Snapshot`，后期难以维护。
8. 把远端 Replica 的所有 Collider 都禁用，导致攻击判定、锁定和射线检测失效。
9. 服务端依赖 Animator 状态机判断技能阶段或伤害窗口，导致权威逻辑无法在无表现环境运行。

## 后续开放问题

以下问题不阻塞移动同步拆分，但会影响技能和判定模块的具体实现：

1. 攻击判定优先使用 Physics Collider 查询、手写几何查询，还是两者混合？
2. 技能预测失败时，客户端需要硬回滚技能状态，还是只做表现校正？
3. 远端 hurtbox 是否需要按骨骼细分，例如头、身体、四肢，还是先使用整体胶囊体？
4. 单机模式是否也需要保留 Tick 驱动，以便和联机逻辑完全一致？
