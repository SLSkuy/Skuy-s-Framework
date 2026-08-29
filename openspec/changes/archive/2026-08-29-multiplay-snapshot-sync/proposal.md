## Why

单机链路已按「Host 持核、槽位收集意图、再 `Step`」跑通，但联机仍走混乱的 `CharacterReplicationSystem`（自建时钟、预测、插值、生成工厂搅在一起）。需要一条与单机同构、仅快照同步的多人路径：客户端发命令、服务端权威模拟并广播状态、客户端按快照落地。

## What Changes

- 按 `LocalSimulationHost` 的简洁结构增加服务端 / 客户端 Host：各自 `new Simulator()`，`StartSession` 装配实体，`Update` 只驱动核，不把复制神类当世界循环。
- MultiPlay 增加一场一房的成员与所有权、消息收发适配；Join/Leave 只改房间与 Spawn/Unregister。
- 客户端采样本机输入并发送 `Player_Input`；**不**把设备来源挂到 Replica 槽位。己方与远端均为 Replica，姿态只信快照（`Restore`）。
- 服务端把校验后的输入写入 `EntityInputBuffer` 门面并挂到 Authority 槽位；按配置广播 `World_Snapshot`。
- 模拟核：Replica **不**进入本拍 `Step`；提供 `TryRestore`。不实现预测、回滚重放、插值器整模块搬迁。
- **不**整体迁移 `CharacterReplicationSystem` / `CharacterReplicationEntry`；只抽编解码字段对应与缓冲窗口算法。联机入口 **BREAKING**：不再注册或驱动该复制子系统。
- 旧 `ClientSimulator` / `ServerSimulator` 由同构 Session 替换。联机测试 **复用现有 `SyncTestPanel`**（开启服务端 / 开启客户端 / 停止），只改其接线与状态展示，MUST NOT 另起联机调试面板。单机仍走 `LocalPlayTestPanel`。

## Capabilities

### New Capabilities

- `multiplay/snapshot-sync`: 一场一房、命令上行、快照下行、服务端/客户端会话与单机同构的 Host 装配。

### Modified Capabilities

- `simulator`: 按 Role 跳过 Replica 步进；权威网络来源走缓冲消费；无网络的 Restore；Capture 供快照编码使用。

## Impact

- **新增**：`GamePlay.Simulator` 下 Server/Client Host（镜像 LocalHost）；`GamePlay.MultiPlaySystem` 下 Room、NetAdapter、Codec（从 `ProtoUtils` 对应逻辑迁入）、精简 Session。
- **改**：`Simulator.DispatchTick`、`SetInputSource` 与缓冲门面、`MultiPlayManager` 不再 `GetOrRegister<CharacterReplicationSystem>`。
- **删/闲置**：复制系统不再作为联机主循环；本阶段不调用预测与插值整类。
- **测试**：`SyncTestPanel` 仍作为联机入口；状态从复制系统计数改为模拟核 Tick/实体数。`NetworkTestPanel` 保持传输测试，本变更不改。
- **协议**：沿用现有 `Player_Input` / `World_Snapshot` / `Game_Join_*`，不改 `.proto`（除非实现时发现 Join 无法工作，再单列字段）。
