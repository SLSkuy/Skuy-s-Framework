## Why

单机开玩仍从 `GameManager.StartLocal` 直连 `LocalSimulationHost`，战局名册与已删的房间概念脱节，关卡/切图没有可跨地图存活的聚会层。需要先用房间 + 战局管理把「谁在一起」跑通，并让单机走与日后主机相同的编排（进房再开世界），否则流程控制无法接到场景切换和联机。

## What Changes

- 新增游戏房间：成员、容量、房主；房间 MUST 在关卡/场景切换后仍存在，直到显式解散。
- 新增战局管理器：持有房间、把「连接」翻译为房间成员；`playerId` MUST NOT 等于 `clientId` 作为契约。单机用本机假连接进房，MUST NOT 因此拉起 `NetClient`/`NetServer`。
- 将 `GameManager` 收为关卡/流程编排：正式单机入口 MUST 为「建房 → 本机进员 → 再驱动 `LocalSimulationHost`」，MUST NOT 再绕过房间直接开世界。
- 保留 `LocalSimulationHost` 与模拟核为单机世界装配；本阶段 MUST NOT 实现 `ServerSimulationHost`、回环套接字或独立主机。
- 本阶段 MUST NOT 实现 `AppStateManager`、MUST NOT 改应用壳 `GameState` 状态机、MUST NOT 做背包/设置输入模态、MUST NOT 做快照同步。
- 现有 `BattleSession` 名册规则下沉到房间成员表；单机路径不要求接上 Join 协议 Handler。

## Capabilities

### New Capabilities

- `gameplay/room`: 游戏房间为聚会单元：创建/解散、成员加入离开、跨地图存活；单机以假连接进一间一人房。

### Modified Capabilities

- `gameplay/battle`: 战局管理器拥有房间并处理连接抽象；名册与加入/离开以房间为准；加入房间 MUST NOT 单独 Spawn。
- `gameplay/session-flow`: `GameManager` 管理关卡/流程并作为单机玩法入口；MUST 经战局/房间再启动单机 Host；不再以「空闲/进行中/结束」直连 Host 作为唯一合同。
- `simulator/local-play`: 单机 Host 仍不启网络；启动/停止改由「已进房」的流程编排发起，同时至多一套由房间互斥保证。

## Impact

- **GamePlay**：新增房间与战局管理器；整理 `BattleSession`/`BattlePlayer` 与 `GameManager`；`GameCore` 注册战局管理器与 `GameManager`。
- **Simulator**：`LocalSimulationHost` 保留；启动时机改为进房之后。
- **Tests**：`LocalPlayTestPanel` 只请求流程编排（进房开单机 / 解散停世界），展示房间与关卡相位。
- **Network**：本阶段不改传输与 `.proto`；真连接与 Join Handler 留后续。
- **Framework**：不实现 `AppStateManager`，不删改 `GameStateManager` 作为本 change 目标。
