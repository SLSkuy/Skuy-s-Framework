## Context

动机见 `proposal.md`。现状约束：`MessageProcessor` 对每个 `NetEvent` 只存一个 `Action`，`UnRegNetHandler` 删整槽；`ServerSimulationHost`/`ClientSimulationHost`/`ServerHandler`/`ClientHandler` 与 `CharacterReplicationSystem` 都 `Global.Get<NetServer|NetClient>()`；`MatchRoom` 用 `entityId == clientId`；`LeaveClient` 不接连接移除。传输层（`NetServer`/`NetClient`/`ClientManager`/TCP+KCP）可保留。单机参考仍是 `LocalSimulationHost`。行为合同见本 change 的 specs。

## Goals / Non-Goals

**Goals:**

- 分发改为「事件 → Handler 实例列表」，按实例注销。
- 玩法只依赖 Handler、连接事件门面、发送门面。
- 删除联机玩法装配与复制主路径，避免双轨继续长肉。
- `GameManager` 编排单机相位；战局只做玩家名册与加入/离开。

**Non-Goals:**

- 世界快照、`Player_Input` 权威缓冲、预测、插值、Listen-Server、多房间匹配。
- 改 `.proto` 或重写 Transport/`ClientManager` 宽限算法。
- 把 `GameManager` 做成规则/UI/资源的上帝对象。
- 填满 `GameCore.InitDataProxy` 的全部局外数据。

## Decisions

### Decision 1: 传输留下，联机玩法层整段删除

保留 `Network/` 传输与连接抽象。删除运行时联机玩法：`MultiPlayManager`、`ServerSimulationHost`、`ClientSimulationHost`、`MatchRoom`、`ServerHandler`、`ClientHandler`、`CharacterReplicationSystem` 及以其为运行入口的预测/插值接线、`SyncTestPanel`、对应快照同步测试。

`LocalSimulationHost`、`Simulator` 核、`AuthorityInputProvider`/`EntityInputBuffer` 等核侧类型保留，供单机与未来同步复用。

备选：只抽接口、冻结旧 Host。否决：旧路径仍可被面板拉起，耦合不会消失。

### Decision 2: Handler 实例表，而不是覆盖式 Action

`MessageProcessor` 改为按 `NetEvent` 保存处理器列表。引入 `INetHandler<T>`（客户端）与 `IServerNetHandler<T>`（服务端，含 `uint senderId`）。登记返回可丢弃的登记，或提供 `Unregister(instance)`。同一 `T` 多个实例共存。

`NetServer.RegNetHandler` / `NetClient.RegNetHandler` / `Global` 对应 API **BREAKING**：禁止再传入会覆盖槽位的单一 `Action`；连接协议（Ping/Pong/心跳/可靠连接响应）继续在 `Net*` 内部登记，可用同一列表 API，避免两套分发。

玩法 Handler 放在战局/会话模块，构造注入 `IMatchMessenger`（或客户端等价发送口），禁止 `Get<NetServer>()`。本阶段可不实现 Join 协议 Handler 的完整联机闭环，但机制与至少一个示例/测试 Handler 必须可验证多播与按实例注销。

备选：继续 `event Action` 多播但按 token 退订。可作内部实现，对外仍以 Handler 实例为注销键，避免再出现「Bind 时拷贝 delegate 快照」。

### Decision 3: 两个窄门面夹在传输与战局之间

```
Transport / ClientManager
        │
        ▼
IConnectionEvents     （Connected / Disconnected / Removed）
        │
        ▼
BattleSession         （PlayerJoined / PlayerLeft；不持有 NetServer）
        │
        ▼
IMatchMessenger       （按 clientId 或 playerId 可靠发送 / 房间广播）
        │
        ▼
NetServer 适配器
```

`IConnectionEvents` 由 `NetServer` 适配：把 `OnClientRemoved`（及如需的连接建立）转出去。战局只订阅该门面。发送门面内部再查 `ClientManager` 会话。战局 API 使用 `playerId`；门面内部做 `playerId ↔ clientId` 映射可由战局只读投影或门面持有的只读表提供，但映射的写入权在战局。

备选：战局直接订 `NetServer.OnClientRemoved`。否决：与「战局不碰 NetServer」冲突。

### Decision 4: 玩家与连接分离，本阶段不生成 pawn

`BattlePlayer`：`playerId`（战局分配）、可选 `clientId`、相位（Joining/Active/Left）。加入成功只登记玩家。本阶段 **不** Spawn、**不** 绑 `Simulator`。`entityId` 留给后续同步。

`playerId` 使用战局内递增或独立生成，禁止 `playerId = clientId` 作为契约。

容量本阶段可固定为配置或常量（例如 8），超出则拒绝加入；不实现大厅。

### Decision 5: GameManager 只编排相位与 Host

`GameManager`：`SubSystemBase`，相位 `Idle | InPlay | Ending`。`StartLocal()` → `LocalSimulationHost.StartSession()`；`Stop()` 逆序。不持有 `NetClient`。不复用 `GameState` 枚举。

`GameCore` 注册 `GameManager`。`LocalPlayTestPanel` 只调 `GameManager`。`LocalSimulationHost` 去掉对 `MultiPlayManager` 的互斥，改为由 `GameManager` 保证单局。

相机与光标副作用仍留在 `LocalSimulationHost`（已有行为），GameManager 不复制那三行。

本阶段不实现联机 Connecting/Joining 相位（无同步）。

### Decision 6: 删除覆盖式 Global 网络快捷方式或改为实例 API

`Global.RegNetHandler` / `UnRegNetHandler(eventId)` 会误伤整槽，MUST 改为按实例登记或从 `Global` 移除网络登记，避免第三入口继续覆盖。优先：`Global` 不再提供按 event 整槽注销。

## Risks / Trade-offs

- [验收真空] 删除 `SyncTestPanel` 后没有联机可玩验收 → 接受；用 EditMode/PlayMode 测分发、战局名册、GameManager 单机；传输仍用 `NetworkTestPanel`。
- [后续同步返工] 现在不接模拟 → 同步 change 再把 Spawn/缓冲接到 `playerId`。用门面稳住边界，降低返工面。
- [API 破坏] 旧 `RegNetHandler(Action)` 调用点会编译失败 → 本 change 删除调用方或改为 Handler 类。
- [空战局] 加入无 pawn，玩法上「看不见人」 → 单机仍可玩；联机本阶段只保证名册与事件。

## Migration Plan

1. 先改分发与 API，再删联机 Host，避免长时间双轨登记。
2. 删除联机玩法文件与测试后，接入 `GameManager` 与 `BattleSession`。
3. 无法热切：这是玩法层 BREAKING；不同时保留旧 `MultiPlayManager` 入口。
4. 回滚：还原本 change；无数据迁移。

## Open Questions

- 后续同步的验收 GUI 用新面板还是扩展 `LocalPlayTestPanel`：不阻塞本 change。
