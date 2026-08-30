## Why

主菜单按钮与战局管理器都承担了「进房、切场景、开战」的编排，导致 UI 随 `MainScene` 销毁后无法收尾，战局模块也越权做壳层导航。需要把意图交给游戏核心、领域合同留在战局，且不把业务写进按钮。

## What Changes

- 在游戏核心上新增玩法入口方法（单机；联机同理预留），作为主菜单的正式入口：确保本机房间与进房、请求加载玩法场景、场景就绪后请求开战。不新增独立流程子系统，不新增玩法场景引导组件。正式玩法路径从主菜单进入。
- 主菜单面板的 `UI_*` 仅转发到游戏核心对应方法；不持有场景名、不建房、不监听场景事件、不开战。
- **BREAKING**：从 `BattleManager` 移除 `StartLocalPlay` 及场景加载/监听逻辑。战局管理器只保留房间与开战/结束/解散合同。
- 本阶段不实现联机业务；游戏核心预留与单机对称的入口，主菜单 `UI_MultiPlay` 只转发。不引入应用状态管理器。

## Capabilities

### New Capabilities

- `gameplay/play-flow`: 游戏核心上的玩法入口：菜单意图、切第一张玩法图、场景就绪后开战；与战局名册和模拟核解耦。不单独成为子系统。

### Modified Capabilities

- `gameplay/session-flow`: 正式单机开玩由游戏核心编排；战局管理器仍是开战/结束对局合同，不再负责菜单切图。
- `gameplay/battle`: 战局管理器 MUST NOT 加载场景或订阅场景加载事件。
- `simulator/local-play`: 正式 UI 入口经游戏核心到达开战；测试面板仍可逐步调用战局合同，但 MUST NOT 作为主菜单实现。

## Impact

- 代码：`GameCore` 增加单机（及联机预留）入口；精简 `BattleManager`；改写 `MainScenePanel`。不新增 `PlayFlow`，不改 `GameScene` 引导对象。
- API：删除 `BattleManager.StartLocalPlay`；游戏核心提供与按钮对应的玩法方法。
- 依赖：仍用现有 `SceneLoader` / `SceneLoadEvent`、`BattleManager`、`LocalSimulationHost`；不拉起网络栈；不引入 `AppStateManager`。
