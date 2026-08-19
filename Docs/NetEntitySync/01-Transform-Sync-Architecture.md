# 网络对象 Transform 网络同步架构改进方案

## 1. 文档目的

本文档用于指导当前实体系统和网络同步框架的下一轮重构，使其在不更换现有传输层、不迁移 DOTS 的前提下，形成接近成熟网络同步方案的清晰管线。运行时代码可以按职责拆分为多个程序集；程序集边界由依赖关系和模块职责决定，不再把单一 `Assembly-CSharp` 作为约束。

当前阶段只实现网络对象根节点的世界空间 `Position` 与 `Rotation` 同步。玩家、门、动态武器和 NPC 都采用同一网络对象模型；`EntityCharacter` 只是其中一种 Gameplay 宿主。

本文是实现基线。后续会话不应依赖此前讨论，应直接按照本文列出的数据来源、模块归属、文件改动和验收标准实施。

## 2. 结论摘要

当前代码不是一个已经接通但模块化不足的完整网络同步框架，而是若干同步部件的中间状态：

- `TickSystem` 已存在，但没有被任何运行时系统创建或驱动。
- 输入、权威模拟和快照方法已经定义，但没有协议处理器和统一编排者调用。
- `PlayerController` 同时处理输入订阅、预测、历史缓存、回放、同步组件查找、相机和光标，职责过载。
- `NetEntitySyncRoot` 同时承担身份、模块发现、同步工厂、角色切换、控制器装配和更新驱动，正在成为新的中心化依赖点。
- `NetPositionSnapshot` 只保存位置与速度，无法完整恢复产生该 Transform 的模拟状态。
- `NetEntityRole` 把权威归属、本地拥有关系和模拟策略合并成一个枚举，无法自然表达 Host、服务器 AI 等组合。
- 现有 `Player_Input`、`Position_Snapshot` 和 `World_Snapshot` 协议没有接入 `NetClient`、`NetServer` 的消息处理链路。

最终目标不是继续向 `PlayerController` 或 `NetEntitySyncRoot` 添加职责，而是建立“`EntityConfig` 提供实体属性、Prefab 自由组合多个能力组件、网络对象标识负责注册与激活”的可装配网络对象框架，并建立五条单向管线：

1. 输入采样与命令构建。
2. 固定 Tick 模拟。
3. 网络命令和权威快照传输。
4. 本地预测、恢复与重放。
5. 非拥有实体的表现插值。

Gameplay 层的目标使用方式是：`EntityCharacter` 只引用框架提供的基础能力组件（例如 Transform、输入、模拟和同步端口），Controller 只控制 `EntityCharacter` 的意图入口；Controller 不直接查找网络同步组件、不直接读写快照、不持有 Tick 历史。网络对象是否启用预测、权威或插值，由网络身份、能力组件存在性和运行模式装配。

门和动态武器不应被迫继承 `BaseEntity`。它们可以只有一个网络对象标识组件，再按需求添加多个能力组件，例如 `NetworkTransformCapability`、`NetworkStateCapability`；玩家则在同一基础上额外添加 `NetworkInputCapability`、`NetworkPredictionCapability` 和 Gameplay 模拟组件，并由 `EntityCharacter`/Controller 使用这些接口。

## 3. 设计原则

### 3.1 单一模拟入口

单机、本地预测和服务端权威必须消费相同的 `EntityInputCommand`，调用相同的 `EntitySimulation.Step(tick, deltaTime, command)`。角色差异只决定命令从哪里来、结果发往哪里，不应决定使用哪套移动实现。

### 3.2 网络时间是全局服务

所有实体共享同一个服务端 Tick 时间轴。禁止每个实体自行维护独立 Tick，也禁止用 `Time.time` 或接收时刻代替服务端 Tick。

### 3.3 模拟状态与表现状态分离

- 模拟 Transform 由 `EntitySimulation` 和 `MovementModule` 持有，是预测、校正和服务端权威的依据。
- 表现 Transform 由 `EntityTransformPresenter` 平滑显示，可短暂偏离模拟 Transform，但不能反向影响权威模拟。
- Replica 不运行角色移动模拟，只消费快照并更新表现。

### 3.4 网络复制状态与本地回滚状态分离

网络第一阶段只传输：

- `EntityId`
- `SnapshotTick`
- `LastProcessedInputTick`
- 世界空间 `Position`
- 世界空间 `Rotation`
- 线速度
- 角速度

客户端为了正确重放，必须在本地预测历史中额外保存 `EntityRollbackState`。它至少包含 `MovementModule` 的垂直速度、跳跃次数、冲刺计时、冲刺方向，以及 FSM 当前状态、持续输入状态和输入边沿基线。这些字段第一阶段不通过网络广播，但必须可 Capture/Restore。

### 3.5 传输层不理解实体

`NetClient`、`NetServer`、KCP 和 TCP 只负责消息收发。实体注册、所有权校验、命令排队、Tick 模拟和快照分发全部归 `EntityReplicationSystem`。禁止把 gameplay 逻辑写入 Transport。

### 3.6 先正确，再压缩

第一阶段使用完整快照，明确消息顺序、Tick 语义和回放正确性。位压缩、阈值发送、Delta Compression 和 AOI 在正确性测试通过后另行设计。

## 4. 范围与约束

### 4.1 本阶段包含

- Transform 使用世界空间坐标。
- 服务端拥有最终权威。
- 本地拥有玩家使用客户端预测。
- 远端实体使用快照插值。
- 单机模式走相同的输入命令和模拟入口，但不序列化消息。
- KCP 快速通道承载输入命令和世界快照。
- TCP 可靠通道继续承载连接、加入游戏、生成和销毁等可靠事件。
- Position 与 Rotation 在同一个 Transform 快照内以同一 Tick 原子应用。

### 4.2 本阶段不包含

- Scale、局部坐标和动态父子关系。
- 客户端直接提交 Transform。
- Root Motion 权威移动。
- Rigidbody 预测。
- gameplay 状态同步。
- 丢包条件下的可靠重发。输入命令采用冗余窗口抵抗少量丢包，快照允许丢失。
- Tick 回绕的长期运行优化。实现仍必须使用集中式 Tick 比较工具，避免散落裸 `<=` 判断，为后续回绕支持保留替换点。

## 5. 网络对象模型与能力组件

### 5.1 对象结构：一个身份、多个能力组件

```text
EntityConfig (ScriptableObject, 实体属性)
    -> NetworkObjectIdentity (MonoBehaviour 标识组件)
        + NetworkTransformCapability (可选)
        + NetworkAnimatorCapability (可选)
        + NetworkInputCapability (可选)
        + NetworkPredictionCapability (可选)
        + NetworkStateCapability (可选)
        + optional EntityCharacter / Gameplay Controller / Presenter
```

- `EntityConfig` 继续作为实体属性配置来源，保存移动、旋转、碰撞等 Gameplay 参数，不负责声明网络能力组合。
- `NetworkObjectIdentity` 是 GameObject 上唯一的网络对象标识和装配入口，保存 `NetworkObjectId`、Owner、Authority、`EntityConfig` 引用及运行模式。
- 每一种网络能力都是独立组件，按需添加到同一个 GameObject 上。Position/Rotation 属于 `NetworkTransformCapability`，动画属于 `NetworkAnimatorCapability`，输入属于 `NetworkInputCapability`，预测属于 `NetworkPredictionCapability`。
- 能力组件可以内部持有普通 C# 子组件，例如命令缓冲、快照插值器和预测历史；这些子组件不需要各自挂载 MonoBehaviour，但能力之间不得通过互相强制类型转换耦合。
- `EntityCharacter` 是可选的 Gameplay 宿主。门可以没有 Character，动态武器可以有武器逻辑宿主，玩家可以有 Character 和 Controller。

### 5.2 能力组件组合规则

Prefab 通过组件组合表达网络能力，`NetworkObjectIdentity` 在启用时扫描同一 GameObject 上的能力组件，校验依赖并向 `EntityReplicationSystem` 注册。

示例：

| 对象 | 能力 | 模式 | Controller |
| --- | --- | --- | --- |
| 玩家 | `NetworkTransform` + `NetworkInput` + `NetworkSimulation` + `NetworkSnapshot` + `NetworkPrediction` | Predicted（拥有端）/Authoritative（服务端）/Interpolated（其他客户端） | `PlayerController` |
| 门 | `NetworkTransform` + `NetworkSimulation` + `NetworkSnapshot` | Authoritative（服务端）/Interpolated（客户端） | 无或 `InteractableController` |
| 动态武器 | `NetworkTransform` + `NetworkSimulation` + `NetworkSnapshot`；若由玩家本地操作则额外 `NetworkInput`/`NetworkPrediction` | Authoritative 或 Predicted，按 Owner 策略解析 | `WeaponController` 或所属角色命令源 |

门的“静态”是 Gameplay 运动策略静态，不代表不能同步：门仍然拥有网络对象 ID、服务端状态和 Transform 快照。动态武器同样不应复用玩家 Controller，只需使用相同基础 Transform/同步能力。

### 5.3 标识与覆盖规则

推荐优先级：

1. 框架默认能力依赖和安全约束。
2. Prefab 上显式添加的能力组件。
3. `EntityConfig` 提供的实体属性。
4. 运行时网络分配的 `OwnerClientId`、Authority 和 SimulationMode。

运行时不得修改能力集合；对象池复用时只重置运行模式、ID 和状态，不能把上一个对象的能力泄漏到下一个对象。新增能力的编辑器工作流是：在 Prefab 上添加对应能力组件并满足其依赖。

## 6. 当前数据来源与代码位置

| 数据 | 当前来源 | 当前使用位置 | 改造后的唯一入口 |
| --- | --- | --- | --- |
| 本地输入 | `GameCore.Instance.LocalInput` / `IInputStateProvider` | `PlayerController.Start`、事件订阅、`CaptureInput` | `LocalEntityInputSource.Sample` 构建 `EntityInputCommand` |
| AI 意图 | `AIController`，当前为空 | 尚未形成管线 | `AIEntityInputSource.BuildCommand` |
| 固定 Tick 配置 | `SyncConfig.Instance.simulationTickRate` | `TickSystem`、`NetPositionSync` | `NetworkTickSystem` |
| 快照频率 | `SyncConfig.Instance.snapshotTickRate` | 当前未被运行时消费 | `EntityReplicationSystem` 的服务端快照调度 |
| 插值延迟 | `SyncConfig.Instance.interpolationDelayTicks` | `NetPositionSync` | `SnapshotInterpolationSystem` |
| 实体配置 | `EntityConfig.Instance` | `BaseEntity.InitConfig` | 保持现状，由实体模拟初始化读取 |
| Position | `MovementModule.transform.position` | `NetPositionSync.CaptureSnapshot` | `EntitySimulationState.Position` |
| Rotation | 当前没有统一所有者 | 仅配置中存在旋转速度 | `RotationModule` / `EntitySimulationState.Rotation` |
| 协议定义 | `Assets/Scripts/GamePlay/Protocol/net_sync.proto` | `NetSyncUtils` 转换 | 保持为协议唯一来源，重新生成 `Generated/NetSync.cs` |
| 传输 | `NetClient`、`NetServer`、KCP/TCP Transport | 通用消息收发 | 保持通用，仅由复制系统注册业务处理器 |

## 6. 目标模块与目录

后续新代码优先迁移到新的职责目录和对应程序集，不再向已经完成阶段迁移的旧目录追加同步职责。程序集可以新增或拆分，但必须保持依赖方向清晰：纯数据/模拟核心不依赖 Unity 场景和传输层；Gameplay 依赖核心；客户端/服务端复制层依赖核心和网络传输；测试程序集只依赖被测程序集。

建议目录如下：

```text
Assets/Scripts/GamePlay/EntitySystem/
├── Core/
│   ├── EntityInputCommand.cs
│   ├── EntitySimulationState.cs
│   ├── EntityRollbackState.cs
│   ├── IEntitySimulation.cs
│   └── IEntityStateStore.cs
├── Simulation/
│   └── EntitySimulation.cs
├── Input/
│   ├── IEntityInputSource.cs
│   ├── LocalEntityInputSource.cs
│   └── AIEntityInputSource.cs
├── Modules/
│   └── Transform/
│       ├── MovementModule.cs
│       ├── RotationModule.cs
│       └── EntityTransformPresenter.cs
└── Sync/
    ├── NetworkObjectIdentity.cs
    ├── EntityAuthority.cs
    ├── EntitySimulationMode.cs
    ├── EntityReplicationSystem.cs
    ├── Command/
    │   ├── EntityCommandBuffer.cs
    │   └── ServerEntityCommandQueue.cs
    ├── Prediction/
    │   ├── EntityPredictionRunner.cs
    │   └── PredictedFrameBuffer.cs
    ├── Snapshot/
    │   ├── EntityTransformSnapshot.cs
    │   ├── SnapshotBuffer.cs
    │   └── SnapshotInterpolator.cs
    └── Timing/
        ├── NetworkTickSystem.cs
        └── TickUtils.cs
```

目录表示职责边界，不表示创建程序集边界。实现时若某个类型只被一个文件使用，可先保持为文件内私有类型，避免为了目录对称制造抽象。

### 6.1 基础组件与 Gameplay 的依赖方向

目标依赖方向固定为：

```text
Framework Sync Components
        ^
        |
EntityCharacter / Gameplay Modules
        ^
        |
Entity Controllers / Input Sources
```

- 基础同步组件不引用 `PlayerController`、`EntityCharacter` 的具体派生类型或任何场景相机逻辑。
- `EntityCharacter` 可以引用基础组件接口或同一实体上的能力组件，例如 `IEntityTransformComponent`、`IEntitySimulationDriver`，但不应引用 `NetClient`/`NetServer`。
- Controller 依赖 `IEntityIntentReceiver` 或 `IEntityCommandTarget`，只提交 Move、Aim、按钮等意图；它不拥有模拟状态。
- `EntityReplicationSystem` 依赖组件接口和实体注册接口，不依赖具体 `PlayerEntity`、`MonsterEntity` 或 `RemotePlayerEntity`。
- 表现组件可以读取同步组件的只读状态，但不能将插值结果写回权威模拟状态。

### 6.2 基础组件的最小集合

第一阶段不把每个网络角色做成一个 Controller 类型，而是由以下基础组件组合能力：

| 组件类别 | 作用 | 权威侧 | 预测侧 | 插值侧 |
| --- | --- | --- | --- | --- |
| `NetworkObjectIdentity` | NetworkObjectId、Owner、Authority、EntityConfig、激活标识 | 注册 | 注册 | 注册 |
| `EntityTransformComponent` | Position/Rotation 模拟状态与安全应用 | 写入 | 预测写入/校正 | 只读快照应用 |
| `EntityInputComponent` | 当前 Tick 命令和命令来源 | 消费队列 | 采样/缓存 | 不启用 |
| `EntitySimulationComponent` | 调用统一 `Step` 并 Capture/Restore | 启用 | 启用 | 禁用 |
| `EntityPredictionComponent` | 命令历史、确认、回放 | 禁用 | 启用 | 禁用 |
| `EntitySnapshotComponent` | 快照捕获、接收、排序 | 捕获 | 接收权威 | 接收缓冲 |
| `EntityInterpolationComponent` | 服务端时间轴采样 | 禁用 | 禁用 | 启用 |
| `EntityTransformPresenter` | 视觉平滑和模型表现 | 可选 | 启用纠错平滑 | 启用插值 |

这里的“组件”是运行时职责组件，可以是 `MonoBehaviour`、普通 C# 对象或由一个装配器创建的对象；是否挂载到 GameObject 不应成为网络协议的隐含条件。第一阶段可以沿用 MonoBehaviour 以兼容现有 Prefab，但同步算法和状态缓冲不得依赖 Unity 生命周期回调。

## 6.3 标识驱动的同步组件激活

“通过标识自动激活”需要区分三类标识，不能用一个 `ModuleType` 同时表达所有含义：

### A. 能力标识 `NetworkObjectCapabilityId`

表示网络对象拥有哪种基础能力，例如：

```text
TransformPosition
TransformRotation
InputCommand
Simulation
Snapshot
Prediction
Interpolation
```

它决定创建哪些基础组件和协议 Channel。

### B. 运行模式标识 `NetworkObjectSimulationMode`

表示本实例当前如何运行：`Local`、`Predicted`、`Authoritative`、`Interpolated`。它决定已创建组件中哪些被启用，不改变实体能力集合。

### C. 网络同步 Channel 标识 `NetworkObjectSyncChannelId`

表示某个能力如何复制，例如第一阶段的 `Transform` Channel。它用于快照注册、捕获、应用和发送频率，不应使用 C# 类型强制转换作为唯一注册方式。

推荐激活流程：

```text
Prefab 上的能力组件集合
    -> NetworkObjectIdentity 注册网络对象
    -> NetworkObjectIdentity 扫描并激活多个独立能力组件
    -> Mode Resolver 根据 Authority + Ownership 设置运行模式
    -> SyncChannelRegistry 按 ChannelIds 激活捕获/接收/插值端口
```

装配器必须具备以下约束：

- 同一能力标识最多激活一个实现；重复实现报错，不静默覆盖。
- 标识到实现的注册表在框架初始化时建立，禁止 Gameplay 文件修改全局工厂字典。
- 缺少能力依赖时给出明确错误，例如 `Prediction` 必须依赖 `InputCommand`、`Simulation` 和 `Snapshot`。
- 运行模式切换只启用/停用能力，不动态替换 Controller 类型。
- Prefab 上的显式组件优先，自动创建只补齐缺失基础组件；所有自动创建结果必须可在 Inspector/调试面板查看。
- 激活和销毁必须幂等，支持对象池复用和断线重连。

实现技术可以在后续从以下两种方案中选择，但语义必须保持一致：

1. `Flags enum + Registry`：适合静态基础组件集合，编译期约束较弱但实现简单。
2. Prefab 组件组合：能力集合直接由 Inspector 上的能力组件表达，适合当前项目的 Prefab 工作流。

不建议第一阶段使用全量反射扫描任意 `INetSyncComponent`。反射会隐藏依赖、增加启动顺序问题，并让协议 Channel 与组件能力难以审计。

## 5.4 同一项目内的客户端世界与服务端世界

客户端和服务端都实现于本项目，但必须拥有不同的运行时世界职责。两者可以共享类型和模拟规则，不能共享同一个实体实例的模拟所有权。

```text
客户端世界：LocalInputSource
    -> EntityInputCommand
    -> ClientEntityCommandBuffer
    -> 本地预测 EntitySimulation.Step
    -> 上传输入批次
    -> 接收权威快照
    -> Reconcile / Rollback / Replay

服务端世界：NetworkTransport 接收输入
    -> ServerEntityCommandQueue 校验、归属检查、去重
    -> 权威 EntitySimulation.Step
    -> 捕获 EntityTransformSnapshot
    -> 广播 WorldSnapshot
```

共享层只包含 `EntityInputCommand`、`EntitySimulation.Step`、`EntitySimulationState`、回滚状态契约、快照数据结构和 Tick 比较规则。服务端专属逻辑包括连接到实体的归属表、命令合法性校验、命令队列和快照广播；客户端专属逻辑包括本地输入采样、预测历史、回滚重放和远端插值。`NetClient`/`NetServer` 仍只负责传输，不能直接推进实体模拟。

Host 模式必须以服务端世界为唯一权威模拟源。Host 上的本地客户端视图可以消费服务端结果，但不得再对同一个实体执行第二次权威 `Step`；客户端预测实例与服务端实体实例必须通过明确的 `NetworkObjectId` 映射区分。

## 5.5 面向技能系统的扩展边界

技能不能作为 `EntityCharacter` 的网络特例，也不能把技能状态塞进 Transform 快照。未来技能应作为独立能力组件和独立同步 Channel 接入：

- `NetworkSkillCapability`（概念名称）负责技能命令入口、技能状态注册和网络模式装配；它依赖 `NetworkInputCapability`/`NetworkSimulationCapability`，可选依赖 Transform、Animation 或资源能力。
- Controller 只生成技能意图或技能命令，例如 AbilityId、目标实体、目标方向和按键边沿，不直接修改冷却、施法阶段或伤害结果。
- 服务端验证技能归属、冷却、资源、目标和距离，并在权威 Tick 中推进技能模拟；重复命令按 `EntityId + InputTick + CommandSequence` 保证幂等。
- 客户端只预测允许预测的本地技能阶段或表现，收到服务端确认后按技能回滚状态执行 Reconcile/Replay；服务端不下发客户端预测所需的全部内部临时字段。
- 技能状态和技能事件使用独立 Channel，与 Transform Channel 共享 `NetworkObjectId` 和 `SnapshotTick`，但拥有独立的捕获、应用、丢弃和版本策略。不要继续扩大单一巨型 `WorldSnapshot`，应使用按实体和 Channel 分组的可扩展快照载荷。

技能回滚状态至少要能恢复 AbilityId、施法阶段、剩余冷却、资源消耗结果和确定性事件序号；动画、音效、特效属于表现事件，不能作为权威模拟状态。未来生命、背包、动画等能力沿用相同的“命令/模拟状态/复制状态/表现事件”分层，不修改 Transform 预测管线。

## 7. 核心数据模型

### 7.1 `EntityInputCommand`

一个命令代表一个完整模拟 Tick，不再由 Controller 比较上一帧输入推导边沿。

建议字段：

```text
Tick
Move
Aim
ButtonsHeld
ButtonsPressedThisTick
```

`Jump`、`ToggleRun` 等一次性动作写入 `ButtonsPressedThisTick`。`Sprint` 等持续动作写入 `ButtonsHeld`。同一命令可以在客户端预测和服务端权威模拟中重复消费，不能依赖 Unity 帧事件是否恰好发生。

### 7.2 `EntitySimulationState`

它是跨网络比较和应用的 Transform 权威状态：

```text
Position: Vector3
Rotation: Quaternion
LinearVelocity: Vector3
AngularVelocity: Vector3
```

Rotation 统一使用归一化 Quaternion。协议转换后必须再次归一化，并拒绝全零或非有限值 Quaternion。

### 7.3 `EntityRollbackState`

它只存在于本地预测历史，用于在权威快照到达后准确恢复模拟：

```text
TransformState
FSM state key
Movement vertical velocity
Movement last direction and locomotion speed
Jump count
Dash active, direction and remaining time
Persistent locomotion flags
Previous command/button baseline when still required
```

实现 `CaptureRollbackState()` 和 `RestoreRollbackState()` 时必须一次性恢复全部字段，不允许由 Controller 逐项猜测或只调用 `Teleport`。

### 7.4 `EntityTransformSnapshot`

快照是网络传输边界：

```text
EntityId
SnapshotTick
LastProcessedInputTick
Position
Rotation
LinearVelocity
AngularVelocity
```

`LastProcessedInputTick` 对非拥有实体可以为 `0`；对本地拥有实体用于裁剪已确认命令并确定重放起点。

### 7.5 `EntityCharacter` 与 Controller 契约

`EntityCharacter` 是 Gameplay 的实体行为外观，不是网络身份，也不是同步根。其职责固定为：

- 持有或引用实体配置、FSM 和 Gameplay 能力模块。
- 接收 Controller 提交的意图/命令。
- 委托给 `EntitySimulationComponent` 执行 Tick 模拟。
- 提供只读状态给动画、UI、调试和同步组件。

`EntityCharacter` 不得：

- 直接订阅网络消息。
- 直接访问 `NetClient`、`NetServer` 或 Transport。
- 直接处理 Prediction/Reconcile/Interpolation。
- 依赖 `NetEntityRole` 决定是否运行 `Update`。

Controller 的职责固定为：

- 选择输入来源（本地输入、AI、重放或测试）。
- 将输入来源映射为 `EntityInputCommand`。
- 调用 Character 的意图入口或向命令目标提交命令。
- 在生命周期结束时解除输入订阅。

Controller 不得：

- 直接写 `Transform`、`MovementModule` 私有状态或 FSM 状态。
- 直接持有快照缓冲和预测历史。
- 按 `Local/Predict/Authority/Replica` 创建四套不同实体控制实现。
- 设置 Camera、Cursor 或表现平滑参数。

测试驱动器可以拥有一个测试 Controller，但它必须实现与正式 Controller 相同的 `IEntityInputSource`/`IEntityCommandTarget` 契约，不能复制一套 `ServerPlayerController`、`ClientPlayerController`、`RemotePlayerController` 的旧算法。

## 8. 身份、权威与模拟模式

删除用一个 `NetEntityRole` 表达全部含义的做法，拆成两个维度：

### 8.1 `EntityAuthority`

```text
Server
```

第一阶段只有服务端权威。保留枚举或明确类型是为了让权限判断集中化，不表示当前支持客户端权威。

### 8.2 `EntitySimulationMode`

```text
Local
Predicted
Authoritative
Interpolated
```

- `Local`：单机，采集本地命令并直接模拟。
- `Predicted`：本地拥有客户端，采集命令、立即模拟、上传命令、接收权威校正。
- `Authoritative`：服务端，从命令队列消费命令并模拟。
- `Interpolated`：非拥有客户端，不运行 gameplay 模拟，只插值表现。

`NetworkObjectIdentity` 只保存 `NetworkObjectId`、`OwnerClientId`、Authority、`EntityConfig` 和 SimulationMode，并负责扫描能力组件、向 `EntityReplicationSystem` 注册/注销。它不创建 Controller，不持有具体同步算法，也不在 `Update` 中遍历并驱动同步模块。

## 9. Tick 与更新顺序

### 9.1 全局顺序

每个渲染帧按下列顺序推进：

```text
NetClient / NetServer 更新传输并投递已收到消息
    -> NetworkTickSystem 累积 deltaTime
        -> 可能执行 0..N 个固定 Tick
            -> 客户端采样/复用 EntityInputCommand
            -> 服务端消费 EntityInputCommand
            -> EntitySimulation.Step
            -> 服务端按 snapshotTickRate 捕获并广播 WorldSnapshot
    -> SnapshotInterpolator 使用渲染时间更新远端表现
    -> EntityTransformPresenter 应用预测纠错平滑
```

### 9.2 Tick 语义

- `SimulationTick`：执行模拟步骤的编号。
- `InputTick`：命令将被执行的模拟 Tick；第一阶段与客户端预测 Tick 相同。
- `SnapshotTick`：服务端捕获状态时已完成的模拟 Tick。
- `LastProcessedInputTick`：该实体在服务端已实际执行的最后一个客户端输入 Tick。

快照必须在对应服务端模拟 Tick 完成后捕获，不能在 Tick 增加但模拟尚未执行时读取 Transform。

### 9.3 追赶策略

`NetworkTickSystem` 可以限制单帧最大 Tick 数以避免螺旋，但不能静默把客户端和服务端时间轴当作仍然同步。超过限制时必须记录指标，并由客户端网络时钟逐步校正预测 Tick。第一阶段至少暴露：

- 当前本地 Tick。
- 最近服务端 Tick。
- 预测领先 Tick 数。
- 本帧执行 Tick 数。
- 丢弃或限制的 Tick 数。

## 10. 输入命令管线

### 10.1 客户端

1. `LocalEntityInputSource` 在 Unity 帧内缓存当前持续输入和按键边沿。
2. 每个模拟 Tick 构建一个 `EntityInputCommand`。
3. `EntityPredictionRunner` 将命令写入环形 `EntityCommandBuffer`。
4. 相同命令立即交给 `EntitySimulation.Step`。
5. 通过 KCP 上行最近若干个尚未确认命令，而不是只发送当前一个命令。

冗余窗口建议从 3 个命令开始，最终由丢包测试调整。服务端按 `EntityId + InputTick` 去重，因此重复命令不会重复模拟。

### 10.2 服务端

1. `EntityReplicationSystem` 接收 `PlayerInputBatch`。
2. 根据连接的 `clientId` 查找它拥有的实体，禁止信任客户端任意填写的 `EntityId`。
3. 校验 Tick 窗口、字段有限性、输入向量长度和按钮位。
4. 将有效命令插入该实体的有界有序队列。
5. 每个权威 Tick 最多执行目标 Tick 对应的一条命令。

缺失命令策略在第一阶段固定为：持续字段沿用最后一个已知命令，一次性按钮清零。不能因为队列为空而完全停止服务端模拟，否则重力等连续模拟也会停住。

## 11. 权威快照管线

服务端在 `snapshotTickRate` 对应的模拟 Tick 之后构建一个 `WorldSnapshot`，其中包含本阶段所有可观察实体的 `EntityTransformSnapshot`。

第一阶段未实现 AOI，因此广播给所有已进入游戏的客户端。消息通过 KCP 发送，允许丢包和乱序：

- 客户端按 `SnapshotTick` 丢弃过旧的 WorldSnapshot。
- 单实体快照缓冲按 Tick 排序并覆盖重复 Tick。
- Position 与 Rotation 必须来自同一次 Capture，不允许分别到达和应用。
- 快照不触发 gameplay 事件，只更新预测权威基线或 Replica 插值缓冲。

## 12. 本地预测与权威校正

### 12.1 预测历史

每个预测 Tick 保存：

```text
InputTick
EntityInputCommand
模拟完成后的 EntityRollbackState
```

缓冲容量必须覆盖最大可接受 RTT、抖动和安全余量。配置继续来源于 `SyncConfig`，但字段应从含糊的 `maxBufferedInputs` 重命名为明确的 `predictionHistorySize`。

### 12.2 收到权威快照

1. 拒绝错误 `EntityId`、过旧 Tick、非有限数值或非法 Quaternion。
2. 找到 `LastProcessedInputTick` 对应的本地预测帧。
3. 比较权威 Transform 与该 Tick 的预测 Transform，而不是与当前帧比较。
4. 差异低于阈值时只裁剪已确认历史。
5. 差异超过阈值时，恢复权威 Transform 和该 Tick 对应的完整回滚基线。
6. 从 `LastProcessedInputTick + 1` 到当前预测 Tick，按原顺序重放尚未确认命令。
7. 模拟 Transform 立即得到正确结果；表现 Transform 使用短时误差衰减，避免视觉瞬移。

如果历史中找不到确认 Tick，禁止用环形数组当前槽位猜测。应执行一次硬校正、清空历史并记录 `PredictionHistoryMiss` 指标。

### 12.3 校正阈值

Position 与 Rotation 分别配置阈值：

- Position 使用世界空间距离。
- Rotation 使用 `Quaternion.Angle`。

阈值只决定是否回滚，不决定是否接受服务端权威。服务端状态始终是最终依据。

## 13. 远端插值

`SnapshotInterpolator` 只处理 `Interpolated` 实体：

1. 维护按服务端 Tick 排序的有界缓冲。
2. 渲染时间保持在最新服务端时间之后固定延迟的位置。
3. Position 使用 `Vector3.Lerp` 或基于速度的 Hermite 插值；第一阶段先使用 `Vector3.Lerp`。
4. Rotation 使用最短路径 `Quaternion.Slerp`。
5. 快照不足两个时保持最近有效状态。
6. 缓冲耗尽时第一阶段停止在最后快照，不进行无限外推。
7. 首个快照、生成和大距离传送使用 Snap，并清空旧缓冲。

插值器不得写入预测实体，不得调用 FSM，不得创建或切换碰撞组件。Replica 的碰撞和查询策略由实体装配阶段一次性配置。

## 14. Rotation 所有权

当前 `MovementModule` 只写 Position，Rotation 没有统一所有者。实现前必须确定并固化以下规则：

- `RotationModule` 是模拟 Rotation 的唯一写入者。
- `Aim` 或移动方向被转换为每 Tick 的目标 Rotation。
- `RotationModule.Step(command, deltaTime)` 根据 `EntityConfig` 的旋转速度推进 Quaternion。
- `EntityTransformPresenter` 可以对模型子节点做额外视觉朝向，但网络同步的是实体根节点世界 Rotation。
- Camera orientation 不是实体权威 Rotation，不直接写入快照。
- Animator Root Motion 本阶段不得覆盖根节点 Position/Rotation；如需启用，必须先另立 Root Motion 权威设计。

## 15. 协议调整

修改 `Assets/Scripts/GamePlay/Protocol/net_sync.proto`，不要手改 `Generated/NetSync.cs`。协议建议调整为：

```proto
message Quaternion {
  float x = 1;
  float y = 2;
  float z = 3;
  float w = 4;
}

message Player_Input_Command {
  uint32 input_tick = 1;
  Vec2 move_input = 2;
  Vec2 aim_input = 3;
  uint32 buttons_held = 4;
  uint32 buttons_pressed = 5;
}

message Player_Input_Batch {
  uint32 entity_id = 1;
  repeated Player_Input_Command commands = 2;
}

message Entity_Transform_Snapshot {
  uint32 entity_id = 1;
  uint32 snapshot_tick = 2;
  uint32 last_processed_input_tick = 3;
  Vec3 position = 4;
  Quaternion rotation = 5;
  Vec3 linear_velocity = 6;
  Vec3 angular_velocity = 7;
}

message World_Snapshot {
  uint32 snapshot_tick = 1;
  repeated Entity_Transform_Snapshot transforms = 2;
}
```

字段命名统一使用 proto `snake_case`。旧字段在尚未发布兼容协议时直接替换并重新生成；如果已有外部客户端依赖，则必须保留旧 field number 并制定版本迁移方案，不能复用已经发布字段号表达不同语义。

`NetEvent` 保留 `PLAYER_INPUT` 和 `WORLD_SNAPSHOT`。当前未使用的 `PLAYER_SNAPSHOT` 应删除，除非后续明确赋予独立语义。

## 16. 配置调整

`SyncConfig` 应成为同步参数唯一来源，至少包含：

```text
simulationTickRate
snapshotTickRate
maxTicksPerFrame
predictionHistorySize
inputRedundancyCount
interpolationDelayTicks
positionReconcileThreshold
rotationReconcileThresholdDegrees
positionSnapThreshold
rotationSnapThresholdDegrees
```

约束：

- `snapshotTickRate` 不得高于 `simulationTickRate`，或者实现明确的非整数调度器；第一阶段建议要求整除。
- `predictionHistorySize` 必须大于最大允许预测窗口。
- 插值延迟以服务端 Tick 表示，不使用客户端帧数。
- 所有阈值必须可在测试场景中观测和调整。

## 17. 逐文件改造清单

### 17.1 新增文件

| 文件 | 命名空间 | 职责 |
| --- | --- | --- |
| `Core/EntityInputCommand.cs` | `GamePlay.EntitySystem` | 固定 Tick 输入命令和值语义按钮位 |
| `Core/EntitySimulationState.cs` | `GamePlay.EntitySystem` | 网络可比较的 Position/Rotation/速度状态 |
| `Core/EntityRollbackState.cs` | `GamePlay.EntitySystem` | 本地预测完整恢复状态 |
| `Core/IEntityStateStore.cs` | `GamePlay.EntitySystem` | Capture/Restore 回滚状态契约 |
| `Simulation/EntitySimulation.cs` | `GamePlay.EntitySystem` | 唯一 `Step` 模拟编排入口 |
| `Input/IEntityInputSource.cs` | `GamePlay.EntitySystem` | 本地输入和 AI 的命令来源边界 |
| `Input/LocalEntityInputSource.cs` | `GamePlay.EntitySystem` | 从 `IInputStateProvider` 采样并生成命令 |
| `Modules/Transform/RotationModule.cs` | `GamePlay.EntitySystem` | 模拟根节点 Rotation |
| `Modules/Transform/EntityTransformPresenter.cs` | `GamePlay.EntitySystem` | Replica 插值和预测误差视觉平滑 |
| `Sync/NetworkObjectIdentity.cs` | `GamePlay.EntitySystem` | NetworkObjectId、Owner、权威、EntityConfig、模拟模式与注册生命周期 |
| `Sync/EntityReplicationSystem.cs` | `GamePlay.EntitySystem` | Tick、实体注册、协议处理、命令和快照总编排 |
| `Sync/Command/EntityCommandBuffer.cs` | `GamePlay.EntitySystem` | 客户端命令历史与确认裁剪 |
| `Sync/Command/ServerEntityCommandQueue.cs` | `GamePlay.EntitySystem` | 服务端命令校验、去重和顺序消费 |
| `Sync/Prediction/EntityPredictionRunner.cs` | `GamePlay.EntitySystem` | 预测、比较、恢复、重放和校正指标 |
| `Sync/Prediction/PredictedFrameBuffer.cs` | `GamePlay.EntitySystem` | 命令与回滚状态环形历史 |
| `Sync/Snapshot/EntityTransformSnapshot.cs` | `GamePlay.EntitySystem` | 运行时 Transform 快照 |
| `Sync/Snapshot/SnapshotInterpolator.cs` | `GamePlay.EntitySystem` | Position/Rotation 快照采样 |
| `Sync/Timing/NetworkTickSystem.cs` | `GamePlay.NetSync` | 全局固定 Tick 与网络时间状态 |
| `Sync/Timing/TickUtils.cs` | `GamePlay.NetSync` | 集中 Tick 顺序和距离判断 |
| `Sync/Activation/NetworkObjectCapability.cs` | `GamePlay.EntitySystem` | 多个能力组件的公共生命周期与标识契约；不承载所有同步逻辑 |
| `Sync/Activation/NetworkObjectCapabilityId.cs` | `GamePlay.EntitySystem` | 网络对象基础能力标识 |
| `Sync/Activation/NetworkObjectSyncChannelId.cs` | `GamePlay.EntitySystem` | 网络复制 Channel 标识 |
| `Sync/Activation/NetworkObjectComponentActivator.cs` | `GamePlay.EntitySystem` | 扫描 Prefab 上多个能力组件，校验依赖并注册同步 Channel |

### 17.2 修改文件

| 文件 | 必需改动 |
| --- | --- |
| `Entities/BaseEntity.cs` | 移除对 `NetEntitySyncRoot` 的直接依赖；不再同时承担网络身份、意图接收、模拟和状态视图全部角色；对外暴露明确的 Simulation/Input 端口 |
| `Entities/EntityCharacter.cs` | 把状态机注册和模块装配交给 `EntitySimulation`；删除自行选择 `Update` 或 Tick 驱动的双入口 |
| `Entities/EntityContext.cs` | 将命令、持续状态和可回滚状态命名统一；提供 Capture/Restore 所需访问边界 |
| `Modules/Position/MovementModule.cs` | 迁至 Transform 能力目录或保持文件位置但调整职责；提供完整移动状态 Capture/Restore；移除网络角色判断和 Replica 碰撞组件动态创建 |
| `Controller/Player/PlayerController.cs` | 缩减为输入源/玩家装配入口；移除预测数组、回放、快照处理、同步模块查找、相机和光标代码 |
| `Controller/AuthorityController.cs` | 删除，其命令队列和权威模拟职责迁到 `EntityReplicationSystem` 与 `ServerEntityCommandQueue` |
| `Controller/ReplicaController.cs` | 删除，其快照入口迁到 `EntityReplicationSystem`，插值迁到 `SnapshotInterpolator`/Presenter |
| `Controller/EntityControllerUtils.cs` | 用 `EntityInputCommand` 到模拟的明确映射替代上一帧输入比较；若无剩余职责则删除 |
| `Sync/NetEntitySyncRoot.cs` | 由 `NetworkObjectIdentity` 与多个独立能力组件替代；删除同步组件工厂、Controller 动态创建、模块遍历和 `Update` |
| `Sync/NetEntityRole.cs` | 由 Authority 与 SimulationMode 两个维度替代并删除 |
| `Sync/Timing/TickSystem.cs` | 升级或替换为 `NetworkTickSystem`，由 `EntityReplicationSystem` 唯一持有和驱动 |
| `Sync/Snapshot/SnapshotBuffer.cs` | 保留泛型有界缓冲；补充 Tick 比较、乱序、重复、清空和采样测试 |
| `Sync/Config/SyncConfig.cs` | 加入第 16 节配置；修正命名和约束校验 |
| `Sync/TestSimulator/MultiPlayManager.cs` | 保留为测试入口，但只负责启动 NetClient/NetServer 与测试场景，不再持有一套独立同步业务算法 |
| `Sync/TestSimulator/ServerSimulator.cs` | 改为使用 `EntityReplicationSystem` 的服务端驱动接口，实体通过标识自动激活 `Authoritative` 能力 |
| `Sync/TestSimulator/ClientSimulator.cs` | 改为使用 `EntityReplicationSystem` 的客户端驱动接口，拥有实体激活 `Predicted`，远端实体激活 `Interpolated` |
| `GamePlay/Protocol/net_sync.proto` | 引入输入批次与包含 Rotation 的原子 Transform 快照 |
| `Utils/NetSyncUtils.cs` | 改为新协议和运行时结构间的纯转换；校验 Quaternion 和非有限数值 |
| `Events/NetEvent.cs` | 删除无语义的 `PLAYER_SNAPSHOT`，保留输入和世界快照事件 |
| `Network/Client/NetClient.cs` | 不添加实体逻辑；只保证复制系统可注册/注销消息并通过快速通道发送 |
| `Network/Server/NetServer.cs` | 不添加实体逻辑；只保证复制系统可按 clientId 注册处理器、广播和定向发送 |
| `Framework/Common/SubSystemManager/SubSystemPriority.cs` | 复用已有 `NetSyncManager` 优先级，必要时改名为语义一致的 `EntityReplicationSystem` 优先级 |
| 系统启动装配文件 | 在当前真正创建 `SystemManager` 子系统的位置注册 `EntityReplicationSystem`；`Launch.cs`/`MainEntry.cs` 当前只是占位，实施前必须定位实际启动入口，不能假设由它们负责 |

### 17.3 删除文件或旧实现

完成调用点迁移并通过测试后删除旧的同步实现；以下文件只有在新基础组件覆盖其职责后才能删除：

- `NetPositionSync.cs`
- `NetPositionSnapshot.cs`
- `INetSyncComponent.cs`
- `INetSyncSnapshotSource.cs`
- `INetSyncSnapshotReceiver.cs`
- `AuthorityController.cs`
- `ReplicaController.cs`
- `NetEntityRole.cs`
- `NetEntitySyncRoot.cs`

`TestSimulator` 不得删除。它是第一阶段的同步验收夹具，但其旧类型引用必须全部迁移；在迁移完成前，Unity Console 中的编译错误视为阻断项。

不得把旧实现注释保留，不得用 `#if false` 隔离。若 Prefab/Scene 序列化仍引用旧 MonoBehaviour，应先编写一次性迁移工具或手动迁移清单，再删除脚本，避免 Missing Script。

## 18. 系统生命周期与注册

`EntityReplicationSystem` 继承 `SubSystemBase`：

- `Priority` 使用 `SubSystemPriority.NetSyncManager`。
- `Init()` 读取 `SyncConfig`、创建 `NetworkTickSystem` 和各缓冲管理器。
- `BindEvents()` 通过 `Global.RegNetHandler` 注册 `PLAYER_INPUT`、`WORLD_SNAPSHOT` 和加入游戏相关处理器。
- `Update(deltaTime)` 先推进网络 Tick，再更新插值时间；具体顺序必须与 `SystemManager` 对 `NetClient`/`NetServer` 的更新顺序一致。
- `Destroy()` 注销网络处理器、清空实体注册表和历史。

网络对象通过 `NetworkObjectIdentity.OnEnable/OnDisable` 注册和注销。重复 NetworkObjectId 必须报错并拒绝后注册者，不能静默覆盖字典。

## 19. 安全与合法性校验

服务端不能信任客户端提交的数据：

- 连接只能控制其 `OwnerClientId` 对应实体。
- `Move`、`Aim` 必须是有限值并限制长度。
- Tick 必须处于允许的过去/未来窗口。
- 重复 Tick 去重，过旧 Tick 丢弃。
- 每包命令数量限制为配置上限。
- 客户端不能上传 Position 或 Rotation。

客户端也必须校验服务端快照，防止异常数据污染 Transform：

- Vector 和 Quaternion 所有分量必须有限。
- Quaternion 归一化前长度不能接近零。
- `EntityId` 必须存在，未知实体快照可短暂排队或丢弃，但不能自动创建错误类型实体。

## 20. 可观测性

成熟同步框架需要能解释“为什么抖动”。至少记录或展示：

- 本地模拟 Tick、最近服务端 Tick和预测领先量。
- 每秒输入包、命令数、世界快照数和字节数。
- 每实体快照缓冲深度。
- 位置与旋转预测误差。
- Reconcile 次数、Hard Snap 次数、历史缺失次数。
- 服务端命令队列深度、重复命令和非法命令计数。
- Tick 追赶次数和超限次数。

日志只用于异常和诊断，常态指标应进入现有 Debug 面板，避免每 Tick 打印日志。

## 21. 分阶段迁移计划

### 当前实施记录（2026-08-13）

- 阶段 0 已完成：新增 `GamePlay.EntitySimulationCore` 运行时程序集和 `GamePlay.EntitySimulationCore.Tests` EditMode 测试程序集；`NetworkTickSystem`、`SnapshotBuffer`、输入命令位、命令队列和快照排序已有基线测试。
- 阶段 1 已完成（模拟核心迁移）：新增 `EntityInputCommand`、`EntitySimulationState`、`EntityRollbackState`、`MovementRollbackState`、`EntitySimulation`、`RotationModule` 和 `EntitySimulationSystem`；`EntityCharacter` 统一使用 `Step(tick, deltaTime, command)`，移除自身 `Update` 模拟入口；`PlayerController` 不再持有预测帧、相机或光标职责。`EntitySimulation`、`RotationModule` 和 `EntitySimulationSystem` 仍是 `Assembly-CSharp` 中的 Unity 适配层，核心程序集只承载纯数据契约、Tick、快照和命令队列。
- `TestSimulator` 已收敛为网络生命周期测试夹具，不再引用不存在的角色控制器或实现第二套同步算法。
- 阶段 2 已完成：`EntityReplicationSystem` 统一使用 `NetworkTickSystem`；客户端拥有实体每 Tick 只采样并通过 KCP 发送 `PLAYER_INPUT`，本阶段不执行本地预测；服务端校验所有权、Tick 窗口和输入数值后排队，每服务端 Tick 最多消费一条命令并推进权威实体，按配置频率广播 `WORLD_SNAPSHOT`；客户端按快照直接应用本地与远端实体的 Position/Rotation。
- 阶段 2 丢包与乱序处理：服务端未取到当前可执行命令时使用空输入推进，不沿用上一条移动输入，避免输入丢失后持续移动；客户端按实体记录最近应用的 `SnapshotTick` 并丢弃旧快照；客户端输入 Tick 以最近服务端快照 Tick 为锚点，避免客户端与服务端启动时间不同导致全部输入落在校验窗口外。
- 阶段 3 已完成：新增 `GamePlay.NetSync.SnapshotInterpolator` 和 `TransformSnapshot`，Replica 实体按服务端 Tick 缓冲完整 Transform 快照；渲染时间固定落后 `interpolationDelayTicks`，Position 使用 `Vector3.Lerp`，Rotation 使用最短路径 `Quaternion.Slerp`，速度字段同步插值。首个快照直接定位，缓冲不足时保持最近显示状态，重复/乱序快照由 `SnapshotBuffer` 排序覆盖，旧快照由实体 Tick 门禁丢弃。
- 阶段 4 已完成：Predict 实体每个网络 Tick 采样输入、构建命令、立即执行本地 `Step` 并保存 `EntityPredictionFrame`；服务端快照使用 `LastProcessedInputTick` 查找确认帧，按 Position/Rotation 阈值决定是否校正；超阈值时恢复确认帧的完整 `EntityRollbackState`，写入权威 Transform 后按原命令顺序重放未确认帧；确认历史缺失时执行硬校正并清空历史。断开 KCP 后本地 Tick 仍继续预测，恢复连接后通过确认 Tick 收敛。
- 阶段 2 测试入口：`Assets/Scenes/Dev/Network/SyncTest.unity` 挂载 `SyncTestPanel`，运行后可选择“开启服务端”或“开启客户端”。真实端到端测试使用两个独立进程加载同一场景，一个选择服务端，另一个选择客户端；客户端完成 TCP/KCP 握手后自动发送 `GAME_JOIN_REQUEST`，服务端创建 `NetPlayer` 权威实例，客户端收到世界快照后创建本地和远端实例。
- 验证结果：Unity Console 无 C# 编译错误或警告；`SyncTest` Play Mode 面板与子系统初始化冒烟通过；EditMode 测试 10/10 通过。KCP 双进程下的 Position/Rotation 插值可视验收由测试面板执行。
- 验证补充：新增 `EntityPredictionHistoryTests` 覆盖确认帧裁剪、重复 Tick 覆盖和重放帧复制。Unity MCP 当前不可用（HTTP 502），本机 batchmode 因已有编辑器锁/许可证连接失败，需在 Unity Editor 恢复后执行编译、EditMode 和双进程 PlayMode 验收。
- 后续工作：输入批次与每连接速率限制、`GameScene` 中旧同步组件的迁移，以及网络实体工厂与对象池的完整生命周期测试。

### 当前模块目录布局

```text
Assets/Scripts/GamePlay/EntitySystem/
├── Config/
├── Contracts/
├── Controllers/
├── Entities/
├── Input/
├── Modules/
│   ├── Position/
│   └── Transform/
├── Simulation/
└── StateMachine/

Assets/Scripts/GamePlay/EntitySimulationCore/
├── Commands/
├── Contracts/
├── Snapshots/
├── State/
└── Timing/

Assets/Scripts/GamePlay/NetworkSync/
├── Runtime/
├── Config/
├── Interfaces/
├── Snapshot/
├── TestSimulator/
└── Compatibility/
    ├── Controllers/
    └── Position/
```

### 阶段 0：锁定基线

- 为 `SnapshotBuffer`、Tick 调度和协议转换补 EditMode 测试。
- 建立一个本地客户端、一个服务端、一个远端观察者的 PlayMode 测试场景。
- 记录当前 Prefab/Scene 对 `NetEntitySyncRoot`、Controller 和 MovementModule 的引用。

退出条件：现有引用清单完整，测试能稳定复现无延迟基线。

### 阶段 1：统一命令和模拟，不接网络

- 新增 `EntityInputCommand`、`EntitySimulation`、`EntityRollbackState`。
- 单机输入改走固定 Tick `Step`。
- 新增 `RotationModule`，让 Position 与 Rotation 都由模拟拥有。
- 去除 `EntityCharacter.Update` 的第二套模拟入口。

退出条件：单机行为与改造前一致；相同初始状态和命令序列得到相同 Transform 结果。

### 阶段 2：服务端权威闭环

- 接通 `EntityReplicationSystem`、`PLAYER_INPUT` 和 `WORLD_SNAPSHOT`。
- 服务端验证所有权、消费命令并广播 Transform 快照。
- 客户端先不预测，使用服务器快照驱动本地和远端实体。

退出条件：KCP 下 Position/Rotation 完成端到端同步；输入或快照丢失不会卡死持续模拟。

### 阶段 3：远端插值

- 引入统一服务端渲染时间和 `SnapshotInterpolator`。
- Position Lerp、Rotation Slerp。
- 处理生成、传送、缓冲不足和乱序快照。

退出条件：在配置的延迟、抖动和丢包下，远端 Transform 无明显回跳或 Quaternion 长路径旋转。

### 阶段 4：本地预测与校正

- 开启命令历史、完整回滚状态、权威比较和重放。
- 模拟 Transform 硬校正，表现 Transform 误差平滑。
- 加入历史缺失降级路径。

退出条件：本地输入即时响应；延迟和丢包下最终收敛到服务端；跳跃和旋转重放不持续误预测。

### 阶段 5：删除旧链路

- 迁移 Prefab 和 Scene。
- 删除旧 Root、Role、Controller 同步职责和 Position Sync 接口。
- 更新 AGENTS.md 中已经过期的 `MultiPlaySystem` 目录说明。

退出条件：代码库只有一套 Tick、命令、模拟、快照和插值链路，无 Missing Script、无旧协议调用点。

### 阶段 6：多能力组件标识激活

- 增加 `NetworkObjectCapabilityId`、`NetworkObjectSyncChannelId`、多个独立能力组件和 `NetworkObjectComponentActivator`；实体属性继续使用 `EntityConfig`。
- 将 Transform Position/Rotation、Simulation、Snapshot、Prediction、Interpolation 的启用关系从 `NetEntitySyncRoot` 和 Controller 中移出。
- 让 `EntityCharacter` 通过接口引用已激活基础组件；Controller 只引用 Character 的命令入口。
- 将 `TestSimulator` 定位为客户端和服务端 Tick 驱动器：只负责驱动 Tick、连接、命令投递、快照接收和故障注入，不直接实现同步算法，不按网络角色创建 Controller。
- 在同一 Prefab 上验证 Local、Predicted、Authoritative、Interpolated 四种模式的组件启用矩阵。

退出条件：新增一个基础同步能力只需注册标识、实现接口、声明依赖并将组件添加到 Prefab，不需要修改 `EntityCharacter`、Player Controller、服务端流程和 TestSimulator 分支。

## 22. 测试计划

### 22.1 EditMode

- `NetworkTickSystem` 在不同帧率下产生一致 Tick 数。
- 单帧追赶上限和指标正确。
- 命令缓冲按 Tick 存储、覆盖重复项并裁剪确认项。
- 服务端命令队列处理乱序、重复、过旧和过未来命令。
- 服务端拒绝伪造实体归属、越界输入、非法 Quaternion 和重复技能命令。
- `SnapshotBuffer` 处理乱序、重复、容量淘汰和相邻采样。
- Position 插值结果正确。
- Rotation 使用最短路径 Slerp。
- Quaternion 序列化往返后归一化。
- 相同初始状态和命令序列产生相同 Position/Rotation。
- Capture/Restore 后继续模拟与不中断模拟结果一致。
- Tick 比较全部通过 `TickUtils`，覆盖接近 `uint` 回绕边界的案例。

### 22.2 PlayMode

- 单机 Local 模式不依赖网络服务也能固定 Tick 移动和旋转。
- Predicted 客户端输入立即生效，服务端最终状态一致。
- Authority 对无输入 Tick 仍推进重力等连续模拟。
- Interpolated 实体只更新表现，不运行 FSM 或 CharacterController 移动。
- 100 ms RTT、20 ms jitter、1%/5% 丢包下无永久发散。
- 输入包重复和乱序不会重复执行 Jump/Toggle。
- 快照乱序不会回退当前权威 Tick。
- 大距离传送清空旧缓冲并 Snap。
- Host 场景不会对同一实体执行两次模拟。
- 客户端上传输入、服务端权威模拟、快照下发、客户端回滚重放链路在同一项目内端到端运行。
- 服务端在缺少客户端输入时仍推进连续模拟；客户端收到 ACK Tick 后只重放未确认命令。
- 技能能力以独立 Channel 注册时，不修改 Transform 能力和 `EntityCharacter` 的同步职责。
- 技能冷却、施法阶段和事件序号在回滚后保持确定性，重复技能命令不会重复执行。
- 实体销毁后注册表、命令历史和快照缓冲全部释放。
- `TestSimulator` 的 Server/Client 模式均通过基础组件激活矩阵运行，不再引用旧的 `NetPlayerCharacter`、`ServerPlayerController`、`ClientPlayerController` 或 `Player_Snapshot`。
- 同一 Prefab 通过不同标识激活四种运行模式时，组件集合和启用状态符合预期。

### 22.3 Unity 验证

每个脚本阶段完成后：

1. 等待 Unity `is_compiling == false`。
2. 检查 Console Error 和 Warning。
3. 运行相关 EditMode 测试。
4. 运行完整 PlayMode 同步场景。
5. 对场景和 Prefab 检查 Missing Script。

## 23. 验收标准

本设计完成实施的最低标准：

- 运行时存在且仅存在一个全局网络 Tick 来源。
- Local、Predicted 和 Authoritative 使用同一个 `EntitySimulation.Step`。
- 客户端只上传输入命令，不上传 Transform 权威值。
- 服务端按连接所有权校验命令并持续模拟。
- Position 与 Rotation 在同一快照、同一 Tick 捕获和应用。
- Predicted 校正比较确认 Tick 的历史状态，并能完整 Restore/Replay。
- Interpolated 实体使用统一服务端时间缓冲 Position/Rotation。
- `PlayerController` 不包含预测、回放、网络消息、相机或光标逻辑。
- 网络传输层不引用 `BaseEntity`、`MovementModule` 或 Transform 快照运行时类型。
- 旧同步组件和未使用协议入口被删除，没有并行实现。
- `TestSimulator` 只负责测试装配与故障注入，不存在第二套实体同步算法。
- 基础组件可以由标识自动激活，Gameplay 和 Controller 的依赖方向符合第 7.1、8.5 节。
- 延迟、抖动、丢包、乱序和重复消息测试通过。
- Unity Console 无编译错误，场景和 Prefab 无 Missing Script。

## 24. 后续扩展边界

完成本设计后，再按独立设计引入：

- Transform 量化、Delta Compression 和变化阈值。
- AOI / Interest Management。
- 可靠生成、销毁和场景切换状态。
- Gameplay 状态 Channel。
- Lag Compensation 和命中回溯。
- 父子网络实体、载具和局部空间 Transform。
- Root Motion 或 Rigidbody 权威预测。

这些扩展必须复用已经建立的 NetworkTick、EntityId 注册、命令、快照和表现边界，不能重新把职责塞回 Controller 或 Identity。

## 25. 参考方案

本文没有要求引入下列库，而是参考其长期验证过的职责边界：

- Unity Netcode for Entities：区分服务器 Tick、预测 Tick、插值 Tick，并以命令历史和预测回放组织同步。
- Mirror：Network Identity 与按组件同步职责分离，快照插值作为独立算法层。
- FishNet：把 TimeManager、Replicate/Reconcile 和图形平滑分开，并为预测历史与重放建立明确生命周期。
- Unity Netcode for GameObjects：网络身份和传输能力位于 GameObject 工作流之上，但完整预测仍需要业务侧固定 Tick、历史输入、恢复和重放。

参考地址：

- <https://github.com/Unity-Technologies/EntityComponentSystemSamples>
- <https://github.com/MirrorNetworking/Mirror>
- <https://github.com/FirstGearGames/FishNet>
- <https://github.com/Unity-Technologies/com.unity.netcode.gameobjects>

## 26. 成熟方案对照与本项目取舍

### 26.1 Mirror 的取舍

Mirror 将 Network Identity、Transform 同步、同步方向和 Snapshot Interpolation 分成可组合组件，并把 Snapshot Interpolation 算法做成独立、纯 C#、可测试的模块。本文采用同样的边界：网络对象标识、Transform 能力、同步 Channel、插值算法和 Gameplay Controller 分离；不直接复制 Mirror 的 API 或 Weaver 机制。

Mirror 的 NetworkTransform 支持 Position/Rotation/Scale 与 Server-to-Client/Client-to-Server 方向，但本项目第一阶段固定 Server Authority，只上传命令，不接受客户端 Transform。

### 26.2 FishNet 的取舍

FishNet 将 PredictionManager、TimeManager、Replicate/Reconcile 数据和表现插值分开，并区分预测输入状态、重放状态和确认状态。本文沿用“命令历史 + 完整回滚状态 + Reconcile + Replay”的模型，但由于当前继续使用 `CharacterController`，不引入 FishNet 的 Rigidbody 预测组件。

### 26.3 Unity Netcode 的取舍

Unity Netcode for GameObjects 提供网络对象和 Transform 级别能力；Unity Netcode for Entities 进一步将 ServerTick、PredictionTick、InterpolationTick 和预测系统组分开。本文保留项目当前 GameObject/MonoBehaviour 约束，但采纳它们的核心原则：全局 Tick、明确时间轴、网络对象身份与能力组件分离。

### 26.4 本项目最终架构选择

```text
EntityConfig
    + NetworkObjectIdentity (唯一标识组件)
    + NetworkTransformCapability (可选)
    + NetworkAnimatorCapability (可选)
    + NetworkInputCapability (可选)
    + NetworkSimulationCapability (可选)
    + NetworkSnapshotCapability (可选)
    + NetworkPredictionCapability (可选)
        + pure C# Tick / Command / Snapshot / Prediction parts
        + optional Unity Presenter / CharacterController parts
    + optional EntityCharacter (Gameplay simulation host)
    + optional Player/AI/Replay/Test Controller (intent source)
```

该方案的关键收益：

- 门、动态武器和玩家共享同一 Transform/同步基础能力。
- Gameplay 不需要知道网络角色，也不需要直接操作快照。
- Controller 不会因为 Local、Predicted、Authority、Interpolated 增加四套实现。
- 新增基础同步能力时，主要修改能力注册表并将组件添加到目标 Prefab，而不是修改所有实体类和测试驱动器。
- 预测与插值算法可以脱离 Unity MonoBehaviour 做纯 C# 测试，接近成熟框架的验证方式。

## 27. 已确认的架构决策

### 27.1 标识的来源

已确认：不新增网络对象 SO 原型。实体属性继续使用现有 `EntityConfig`；网络能力通过 Prefab 上多个独立能力组件自由组合，`NetworkObjectIdentity` 负责绑定和激活。

### 27.2 自动激活的对象形态

已确认：采用混合模型。编辑器侧以“一个网络对象标识组件 + 任意多个独立能力组件 + 可选 `EntityConfig`”完成同步装配；能力组件内部可以持有普通 C# 模拟、预测、快照和插值对象。只有需要 Unity 生命周期或场景引用的表现/碰撞部分使用 MonoBehaviour。

### 27.3 Controller 的运行模式

已确认：Controller 按输入来源划分，保留 `PlayerController`、`AIController`、`ReplayController`、`TestController` 等；删除按网络角色划分的 `AuthorityController`、`ReplicaController`。网络模式由基础同步组件自动激活。

### 27.4 TestSimulator 的定位

已确认：`TestSimulator` 长期保留，作为客户端和服务端 Tick 驱动器，并承担网络回归测试、延迟/丢包/乱序/重复包和连接生命周期测试。它不拥有网络对象同步算法。

### 27.5 Transform 的空间和物理边界

已确认：第一阶段只同步世界空间 Position/Rotation，继续使用 `CharacterController`；不支持动态父子关系、局部空间同步、Root Motion 权威移动和 Rigidbody 预测。
