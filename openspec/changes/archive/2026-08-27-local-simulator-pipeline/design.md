## Context

动机见 `proposal.md`。约束：运行时改动须符合 `.agents/rules/Architecture.md` 与 `CodingStyle.md`；命名空间 `GamePlay.Simulator` / `GamePlay.EntitySystem` / `GamePlay.MultiPlaySystem` 已存在。已有可复用件：`TickSystem`、`EntityObjectIdentity` / `EntityObjectRole`、`SimulationConfig`、`EntityCharacter.Step`、`EntityCommandBuilder`、`LocalInputProvider`。联机仍由 `CharacterReplicationSystem` 自建 Tick 并 `Step`，本变更不拆尽复制系统，只让 **LocalPlay 不再走复制时钟**。

## Goals / Non-Goals

**Goals:**

- 列出本阶段要实现的 Simulator 子模块，以及留给多人的模块清单与对接面。
- 用「一份模拟核 + LocalHost 装配」跑通生成 → 采样 → 邮箱 → 命令 → `Step`。
- 把后续 ClientHost / ServerHost 需要的窄接口先留在核上（即使本阶段只有 LocalPlay 实现）。

**Non-Goals:**

- 不实现预测、回滚重放、Replica 插值、快照编解码、房间/连接/所有权。
- 删除整棵实体 Controller（含 `PlayerController`），联机预测改为直连 `LocalInputProvider`。
- 不把 `CharacterReplicationSystem` 的权威/预测/插值迁进 Simulator（仅删除 LocalPlay 分支并改引用迁走的共享类型）。
- 不在本设计中解决 Listen-server 双 Role。

## Decisions

### Decision 1: 模块清单（本阶段实现 vs 后续）

大模块仍是 `GamePlay.Simulator` 与 `GamePlay.MultiPlaySystem`。子功能按下表切开。依赖方向固定为 `MultiPlay → Simulator → EntitySystem`，`Simulator` MUST NOT 引用 Net 消息类型。

**本阶段实现（Simulator）**

| 子模块 | 职责 | 主要落点（建议） |
|---|---|---|
| Tick | 唯一固定步长时钟、追帧上限 | 已有 `Tick/TickSystem.cs`，由 `Simulator` 持有并订阅 |
| Registry | `entityId →` 身份、实体、Role、策略槽 | 新建 `Registry/` |
| Mailbox | `entityId + tick → InputState`；缺省空输入 | `Command/`；单机本拍覆盖。权威窗口缓冲从 `CharacterInputBuffer` **迁入**同一目录（改命名空间），复制系统改引用 |
| CommandBuild | `InputState` → `EntityCommand`（边沿按实体保留） | 将 `EntityCommandBuilder` **迁入** `Simulator/Command/`（`EntityCommand` 结构体仍留 EntitySystem） |
| Dispatch | 每 Tick 对已注册实体：取命令 → `EntityCharacter.Step` | `Simulator` 编排，策略对象可先只有 LocalPlay |
| Spawn | 加载原型、Instantiate、`Identity.Init`、`EntityCharacter.Init`、Registry.Register | 将 `PlayerSpawner` **迁入** `Spawn/`，删除 MultiPlay 旧文件；联机 Session 改调新 API |
| Possess | 至多一个 `possessedEntityId`；采样只打给它 | Registry 或 `Simulator` 上的附身 API |
| LocalHost | 启动/停止单机会话、请求 Spawn(LocalPlay)、Possess、绑定相机 | 新建 `Host/LocalSimulationHost.cs` |
| Capture | 转调 `EntityCharacter.CaptureRollbackState` | Registry 查询 + 实体 API |

**本阶段明确不实现（登记对接面即可）**

| 子模块 | 归属 | 将来如何接到本核 |
|---|---|---|
| Prediction | Simulator | Role=Predict 时在 Dispatch 后写入 History |
| Reconcile | Simulator | `ApplyConfirmedState` 触发 Restore + 重放已存 Command |
| Interpolation | Simulator | Role=Replica 跳过 Step，渲染帧采样 |
| ClientHost / ServerHost | Simulator | 与 LocalHost 并列的装配器，Install 不同策略 |
| Room / Connection / Message | MultiPlay | 不进入 Tick |
| Ownership | MultiPlay | 校验后再 `Mailbox.Submit` |
| SnapshotCodec | MultiPlay | 调 Capture / `ApplyConfirmedState` |
| 会话进房夹具 | MultiPlay | 今日的 `ClientSimulator`/`ServerSimulator` 应改名为 Session，本阶段可不改名 |

### Decision 2: 一份 Simulator 核，Host 只做装配

不采用「`LocalSimulator` 与日后 `ServerSimulator` 各写一套 Step」。`Simulator : SubSystemBase` 是唯一时钟持有者。`LocalSimulationHost` 在 `StartSession` 时：确保子系统已 Init、Spawn、Possess。日后 `ServerNetAdapter` 只调用 `SubmitInput` / 订阅 Capture，不 new 第二套 Tick。

备选：LocalHost 自己 new `TickSystem`——否决，会与联机时钟再次分叉。本阶段复制系统仍有自己的 Tick（联机测试未迁走）；**LocalPlay 路径**禁止再订阅复制 Tick。

### Decision 3: 单机邮箱不做网络窗口

权威端 `CharacterInputBuffer` 的「过去/未来 tick 窗口」是延迟与丢包用的。LocalPlay 每拍实时采样，Mailbox 对本拍 `CurrentTick` 写入即可；缺采样则空命令。`SubmitInput(entityId, tick, state)` 签名仍然保留，供日后 MultiPlay 写入历史 tick。

备选：单机直接 `Step(builder.Build(tick, localInput))` 绕过邮箱——否决。绕过会让 Client/Server 无法复用 Dispatch。

### Decision 4: 删除 Controller，输入只走 Provider

删除 `PlayerController`、`AIController`、`EntityControllerBase` 及 `Controllers/` 目录。`NetPlayer` 去掉对应组件。

设备来源仅为 `GameCore.LocalInput`。LocalHost：`GetInputState()` → `Mailbox.Submit`。`PredictOwnedTick` 同样改为采样 `LocalInput`（或 Simulator Possess 邮箱），禁止再 `GetComponent<PlayerController>()`。

相机与光标由 LocalHost Possess/Stop 显式处理。

### Decision 4b: 迁走复用代码，删除死代码

**迁入 Simulator（改引用后删旧文件）：**

- `PlayerSpawner` → `Simulator/Spawn/`
- `EntityCommandBuilder` → `Simulator/Command/`
- `CharacterInputBuffer` → `Simulator/Command/`（联机权威仍用窗口语义；单机 Mailbox 可先薄实现，但不得留下 MultiPlay 第二份缓冲类）

**删除（无引用或已被 Identity/Tick 替代）：**

- 复制系统 `RegisterLocalPlay`、`SimulateLocalPlayTick` 及 `CharacterReplicationEntry` 上的 Controller 字段与启用逻辑
- `NetworkObjectIdentity`、`EntitySimulationMode`、`MultiPlaySystem/Identity/Interface/IEntityObjectIdentity.cs`
- `EntitySystem/Entities/Interface/IEntityObjectIdentity.cs`（若与 Simulator 接口重复且无引用）
- `NetInputProvider`（无调用方；邮箱直接收 `InputState`）
- 确认无 `RegisterSystem` 调用后，删除未使用的 `NetworkTimeSystem` / 仅被其使用的 `SyncConfig`（若仍被资源引用则只删代码入口并去掉创建菜单，避免残留第二套 Tick）

预制体只保留 `EntityObjectIdentity`。联机生成改为 Spawn 后由 Session **显式** `CharacterReplicationSystem.Register`，不再靠旧 Identity 的 `Awake` 自注册。

### Decision 5: 本地实体标识非零

旧复制路径允许 LocalPlay 的 `EntityId == 0` 并用 InstanceID 当字典键。核的注册表统一要求 **非零 entityId**。LocalHost 使用单调分配器（从 1 起）在本会话内分配。为后续联网 ID 空间预留：本机分配器可使用高位或独立区间的注释约定即可，本阶段不必实现全局 ID 服务。

### Decision 6: 原型与 Identity 组件

Spawn 使用现有 Resources 原型（当前 `NetPlayer`）。实例 MUST 带 `EntityCharacter` 与 `GamePlay.Simulator.EntityObjectIdentity`。预制体 MUST 改为 `EntityObjectIdentity` 并移除 `NetworkObjectIdentity`。单机 Spawn 只注册进 Simulator；联机 Session 在 Spawn 后再 `Replication.Register`。

`CharacterPresentationAdapter` 本阶段不是步进所必需；有则忽略插值，角色由模拟直接驱动 Transform。

### Decision 7: 单机入口

`GameCore` 不自动注册玩法子系统（现状）。LocalHost 入口与 `SyncTestPanel` 类似：开发场景面板或显式 `RegisterSystem<Simulator>()` 后 `StartLocalSession()`。本阶段加一个最小 LocalPlay 调试入口（独立面板或在现有开发场景增加「单机」按钮），避免无 UI 则无法验证。联机按钮保持走 MultiPlay，互斥：单机会话运行时 MUST NOT 同时 StartServer/StartClient（可在面板层禁用）。

### Decision 8: 复制系统上的 LocalPlay 分支

删除 `SimulateLocalPlayTick` 与 `RegisterLocalPlay`，不要留死分支。

## Risks / Trade-offs

- [双时钟并存] 本阶段联机复制仍自带 Tick，单机 Simulator 也有 Tick → 缓解：严格按会话互斥；文档标明最终目标是复制改为订阅 Simulator.Tick。
- [预制体缺组件] 去掉 `PlayerController`/`NetworkObjectIdentity` 后联机自注册失效 → 缓解：本变更内改预制体并让 Session 显式 Register。
- [空输入与边沿] 无采样时用空 `InputState` 可能在松键边界少一拍 → 可接受；`EntityCommandBuilder` 按实体持有上一拍 held flags。
- [未迁预测导致联机与单机 Step 入口仍有两处] → 接受为过渡；本核 API 先稳定。

## Migration Plan

1. 补全 Simulator 核与 LocalHost，开发场景可启动单机。
2. 验证移动/视角/跳跃等现有 `EntityCharacter` 能力在单机 Tick 下可用。
3. 确认未启动 MultiPlay 时复制系统不驱动该 pawn。
4. 回滚：移除 LocalPlay 入口与新脚本即可回到仅复制系统的联机测试；不改协议。

后续 change（不在本任务拆解内）：复制系统改为 Simulator 的 Client/Server Host + MultiPlay 传输适配。

## Open Questions

- 单机调试入口放独立面板还是并入 `SyncTestPanel`，不影响规格，实现时按改动面更小的选项选。
- 本机 entityId 是否使用独立高位区间，可在引入联网生成时再定。
