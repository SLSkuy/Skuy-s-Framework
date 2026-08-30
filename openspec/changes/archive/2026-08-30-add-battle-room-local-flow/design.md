## Context

动机见 `proposal.md`。现状：`GameManager.StartLocal` 直接 `new LocalSimulationHost` 并 `Global.Register`；`GameCore` 未注册 `GameManager`（测试面板现注册）；`BattleSession` 名册与单机路径未接；`LocalSimulationHost` 仍是单机世界装配。行为合同见本 change 的 specs。本阶段不实现应用壳状态机、真传输进房或 `ServerSimulationHost`。

## Goals / Non-Goals

**Goals:**

- 战局管理器持有至多一间活动房间，成员 `playerId` 与连接标识分离。
- 单机开玩走「建房 → 本机进员 → `LocalSimulationHost.StartSession`」。
- `GameManager` 表达关卡相位；换关保房间、可拆再建世界。
- `GameCore` 注册战局管理器与 `GameManager`；测试面板只调 `GameManager`。

**Non-Goals:**

- `AppStateManager` / 改 `GameStateManager`。
- 回环 `NetClient`/`NetServer`、Join 协议 Handler 闭环、快照同步。
- 背包/设置输入模态、`timeScale` 暂停策略。
- 以第二张 Unity 场景资源作为换关硬依赖。

## Decisions

### Decision 1: 战局管理器拥有房间，名册不再平行于 Host

`BattleManager : SubSystemBase`。活动房间 `GameRoom` 持成员表（由现有 `BattleSession`/`BattlePlayer` 下沉或内聚，禁止 Host 再维护一套 `playerId`）。本阶段同时至多一间活动房间。

本机进房：`AdmitLocal()` 使用进程内连接标识（常量即可），不经过 `IConnectionEvents` 的 `NetServer` 适配器。离开/解散走同一套名册 API，便于日后把真连接接到同一 `Admit(connectionId)`。

备选：继续让 `BattleSession` 独立于房间。否决：与「房间是聚会单元」重复。

备选：单机仍 `StartLocal` 直连 Host。否决：与房间合同冲突。

### Decision 2: 房间开战才持有流程与模拟核

`BattleManager`：建房、进员、`StartMatch` / `EndMatch` / `Dissolve`。

`BattleRoom.StartMatch()`：本机已有成员时创建 `LocalSimulationHost` 与 `GameManager`，经 `SystemManager.RegisterSystem(instance)` 登记（`GameManager` 无无参构造）。`EndMatch()` 逆序拆除二者，名册留下。`Dissolve()` 先 `EndMatch` 再清名册。

`GameManager`：仅战局内相位 `Idle | InLevel | ChangingLevel`；`ChangeLevel` 重建 Host 世界。不创建房间。

`GameCore` 只注册 `BattleManager`，不注册 `GameManager`。

`LocalPlayTestPanel` 只请求 `BattleManager`：建房进员开战、换关、结束对局、解散房间。

备选：进程级 `GameManager.StartLocalPlay` 去建房。否决：与「房间持有对局」冲突。

### Decision 3: 互斥与注册入口

互斥：已开战则再次 `StartMatch` 失败。`GameCore` 不注册 `GameManager`。

换关本阶段以重建单机世界验证房间存活。

### Decision 4: 本阶段不接 Join Handler

`BattleServerHandler` / `BattleClientHandler` 保持可编译，单机不绑定 Join 协议。真连接进房留后续 change。发送门面要求对「仅本机假连接」不适用；不在本机路径调用 `IMatchMessenger`。

## Risks / Trade-offs

- [假连接与真连接分叉] → 战局管理器对外只暴露 `Admit(connectionId)`，本机常量只是调用方。
- [换关无真实第二场景] → 用相位 + 重建 Host 验收房间存活；场景资源另开。
- [Host 停再开闪一下] → 接受；本阶段优先流程正确。
- [空 Handler 仍抓 `NetServer`] → 本 change 不清理；避免扩大范围。

## Migration Plan

1. 落地房间与战局管理器，单测名册与互斥。
2. 改 `GameManager` 与 `GameCore` 注册；改测试面板。
3. 确认 `StartLocalPlay` 不启网络、换关房间仍在。

回滚：恢复 `StartLocal` 直连 Host（仅应急）；房间类型可删。

## Open Questions

无。本机连接标识具体数值、房间容量常量可在实现时选定，不改变规格。
