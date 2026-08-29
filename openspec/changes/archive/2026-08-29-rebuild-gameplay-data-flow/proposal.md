## Why

当前联机玩法过早叠在未完成的会话数据流上：消息分发按 `NetEvent` 覆盖唯一回调且注销会清掉整槽，战局把连接 `clientId` 当成玩家，Host 与复制系统同时直连 `NetServer`/`NetClient`。需要先修好分发边界、拆掉高耦合联机玩法层，用 GameManager 与战局玩家实体搭完数据流；快照同步留到战局完成之后。

## What Changes

- 将网络消息分发改为可多播、可按 Handler 实例注销；业务消息 SHALL 由独立 Handler 类处理，玩法 MUST NOT 直接调用 `NetServer`/`NetClient` 的收发包与连接表。
- **BREAKING**：删除现有联机玩法框架（`MultiPlayManager`、一场一房快照 Host、`MatchRoom`、`CharacterReplicationSystem` 及其预测/插值接线、`SyncTestPanel` 联机入口）。传输栈（`NetServer`/`NetClient`/`ClientManager`/Transport）保留。
- 客户端新增 `GameManager`：编排玩法会话相位（空闲、进行中、结束），驱动单机 Host；它不是 `GameStateManager`（后者只负责菜单/加载/暂停等应用壳）。
- 服务端新增战局会话：独立的玩家实体（`playerId` 与连接 `clientId` 分离）；连接移除经门面转为战局「玩家离开」事件，战局 MUST NOT 读取 `NetServer`。
- 本阶段 MUST NOT 实现世界快照同步、客户端预测或插值。

## Capabilities

### New Capabilities

- `network/message-dispatch`: 消息按事件路由到 Handler 实例；支持同一事件多个 Handler；按实例注销；连接类协议仍留在传输层。
- `gameplay/session-flow`: 客户端 GameManager 管理玩法流程并编排单机会话；与 `GameStateManager` 职责分离。
- `gameplay/battle`: 服务端战局持有玩家实体与加入/离开；只通过连接事件门面与消息发送口与网络交互。

### Modified Capabilities

- `multiplay/snapshot-sync`: 撤销本阶段联机快照同步与同构联机 Host 的行为要求；该路径从运行时移除。
- `simulator/local-play`: 单机会话改由 GameManager 编排启动/停止；不再依赖联机 Manager 或复制子系统互斥。

## Impact

- **Network**：改 `MessageProcessor`、`NetServer`/`NetClient`/`Global` 的注册注销 API；新增 Handler 契约与连接事件/发送门面。
- **GamePlay**：删除 `MultiPlaySystem` 联机装配与复制主路径；新增 GameManager、战局会话与玩家模型；`LocalSimulationHost` 保留为单机核装配。
- **Tests**：删除或停用 `SyncTestPanel` 与快照同步 EditMode 用例；`LocalPlayTestPanel` 改接 GameManager；`NetworkTestPanel` 仍只测传输。
- **协议**：不改 `.proto`；`Game_Join_*` / `Player_Input` / `World_Snapshot` 可暂不走玩法路径。
- **后续**：战局与会话流稳定后再单开 change 做同步。
