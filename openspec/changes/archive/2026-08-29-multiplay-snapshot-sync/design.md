## Context

动机见 `proposal.md`。单机参考：`LocalSimulationHost` 持有 `Simulator`，`StartSession` 里 Spawn → Register → `SetInputSource` → `StartClock`，`Update` 只 `_simulator.Update`，`StopSession` 逆序拆掉。联机 Host 必须同构，差别只在装配（Authority+缓冲 / Replica+Restore）和谁发网。

旧 `CharacterReplicationSystem` 不得整类接入。可抄：`EntityInputBuffer` 窗口、`ProtoUtils` 字段对应。不要抄：双时钟、Entry 袋子、预测/插值编排、`SetClientEntityFactory`。

## Goals / Non-Goals

**Goals:**

- 与单机相同的 Host 生命周期和核 API。
- 快照同步可玩：两进程（或本机两客户端）能看见权威移动。
- 联机验收只改现有 `SyncTestPanel`，不新建面板。
- 房间只做成员与所有权，不做大厅。

**Non-Goals:**

- 预测、回滚、插值模块搬迁、Listen-server、多房间匹配。
- 改 `net_sync.proto`（默认）；实现卡死再另开字段。
- 保留联机对复制子系统的依赖。
- 新建第二套联机 GUI，或把联机按钮并进 `LocalPlayTestPanel` / `NetworkTestPanel`。

## Decisions

### Decision 1: Host 抄单机骨架，不抄复制系统

```
LocalSimulationHost          ServerSimulationHost         ClientSimulationHost
Init: new Simulator          同左                         同左
Start: Spawn LocalPlay       Join→Spawn Authority         连上→Join；快照→Spawn Replica
       SetInputSource(device) SetInputSource(bufferFacade) 不绑 LocalInput 到槽
       StartClock            StartClock + 快照累加广播      StartClock（Replica 不 Step）
Update: simulator.Update     同左 + Adapter 已在事件里收包   同左；Tick 里采样并 Send
Stop: Unregister Destroy     Room.Leave 全部               清空 Replica
```

`MultiPlayManager` 只选 Server/Client Host + 启停 Net，**不再** `RegisterSystem<CharacterReplicationSystem>`。

`ClientSimulator`/`ServerSimulator` 删除或缩成对 Host 的薄委托，避免第三套「Simulator」命名。

### Decision 2: 一场一房

`MatchRoom`（或同名）：`clientId → entityId`，`TryAuthorize(clientId, entityId)`，Join 分配 entityId（可用 clientId，与现测试一致）。不是房间列表。

### Decision 3: 核只加 Role 门闩和 Restore

`DispatchTick`：Identity.IsReplica 则不 `Collect`/`Step`。其余不变。

`TryRestore` 转调 `EntityCharacter.RestoreRollbackState`。客户端 Host 对快照 `snapshotTick <= lastApplied` 则丢。

权威来源：小类实现 `IInputStateProvider.GetInputState()` = 缓冲 `ConsumeNext` 的输入快照；`Enqueue` 留在具体类。不要让 `EntityInputBuffer.BuildAuthorityCommand` 与槽位 builder 各算一遍边沿——缓冲只出 `InputState`。

### Decision 4: 快照落地用 Restore，不搬插值器

收到更新快照即 Restore。不接 `CharacterSnapshotInterpolator`、不做预测校正。抖动可接受。

Codec：新文件承接 `ToInputState` / `ToPlayerInput` / `ToCharacterSnapshotMessage` / `ToRollbackState`，删掉联机路径对旧复制系统的调用。`lastProcessedInputTick` 可写缓冲的 `LastProcessedTick`，客户端本阶段忽略。

### Decision 5: 客户端输入序号

仍用本地递增 `inputTick`（从 1），与现协议一致，便于以后预测。发送可用 Client Host 订阅的同一 `Tick`（或 StartClock 后在 Dispatch 外由 Host 听 `_simulator` 的 tick——**不要**为发包再 new 一套 TickSystem）。实现上：Simulator 暴露 `event Tick` 转发，或 Host 在 `TickSystem` 同频的 Update 里不敢保证对齐时，允许 Host 在 `Simulator` 增加只读 `Tick` 事件薄转发。优先转发核时钟，禁止第二套追帧器。

设备采样：`GameCore.LocalInput.GetInputState()` 只在 Client Host 发包处调用。

相机：Client 对己方 Replica 绑定 `orientation`（与 LocalHost 相同三行）。

### Decision 6: 旧复制代码

本阶段联机主路径零引用后，删除或掏空 `CharacterReplicationSystem` 的 Init Tick 订阅，避免残留子系统仍 `AdvanceTick`。预测类文件可留着不编译进路径，但不要从新 Host `using`。

### Decision 7: 复用 SyncTestPanel

联机测试入口固定为现有 `SyncTestPanel`：保留「开启服务端 / 开启客户端 / 停止」与单机互斥逻辑，内部仍调 `MultiPlayManager.StartServer/StartClient/Stop`（Manager 改接新 Host）。

`DrawStatus` 改为读 `NetServer`/`NetClient` 与对应 Host 的 `Simulator`（模式、连接、ClientId、模拟 Tick、注册实体、RTT），删除对 `CharacterReplicationSystem` 的展示。不新增场景、不新做联机面板。`LocalPlayTestPanel` 继续只测单机。`NetworkTestPanel` 继续只测传输，本 change 不改。

## Risks / Trade-offs

- [操作延迟] 无预测 → 验收时以快照落地为准，不以本地立刻迈步为准。
- [快照抖动] Restore 每包一跳 → 接受；插值下一 change。
- [Join 与首包快照竞态] Spawn 必须按 entityId 幂等。

## Migration Plan

1. 核：Replica 跳过、Restore、缓冲门面。
2. Room + Codec + Adapter + 两个 Host；Manager 改接线。
3. 停复制 Tick；在现有 `SyncTestPanel` 上开服/开客验收。
4. 单机回归仍走 `LocalPlayTestPanel`。
