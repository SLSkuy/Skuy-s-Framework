## Context

见 `proposal.md` 的 Why。当前 `BattleManager.StartLocalPlay` 订阅场景事件并切图；`MainScenePanel.UI_LocalPlay` 把场景名绑在按钮上。`GameCore` 为 DDOL，已登记 `BattleManager` 与 `SceneLoader`。正式玩法一律从 `MainScene` 进入。禁止 EventBus 匿名 lambda。不引入 `AppStateManager`，单机不拉起网络栈。

## Goals / Non-Goals

**Goals:**

- 游戏核心提供与主菜单按钮对应的玩法入口：单机完整编排；联机方法本阶段可为空实现。
- 切图与「场景就绪后开战」留在游戏核心（跨场景存活），不放战局、不放面板、不放玩法场景引导。
- 战局只保留房间与开战/结束/解散。

**Non-Goals:**

- 不新增 `PlayFlow` 或其它玩法流程子系统。
- 不在 `GameScene` 增加引导组件；不把直接打开玩法场景当作正式入口。
- 不实现联机业务与对局内换图。
- 不改模拟核与名册规则。

## Decisions

### 1. 玩法入口是 `GameCore` 的方法，不是新子系统

- **选择**：例如 `StartLocalPlay()` / `StartMultiPlay()`（名称以实现时编码风格为准）。场景名由 `GameCore` 持有。`UI_LocalPlay` → `GameCore.Instance.StartLocalPlay()`，与现有 `QuitGame` 同一层。`StartMultiPlay` 本阶段空实现，供 `UI_MultiPlay` 转发。
- **理由**：主菜单只应碰到游戏核心；后续联机同一模式，不必先发明流程类型。
- **备选**：独立 `PlayFlow` — 用户明确不需要多一层。战局 `StartLocalPlay` — 名册模块不该切图。

`StartLocalPlay` 行为：

1. 经 `BattleMgr` 确保本机房间与进房。
2. 已在约定玩法场景 → `BattleMgr.StartMatch()`。
3. 否则订阅场景完成/失败（**具名方法**），再 `SceneMgr`/`Global.LoadScene`。完成后开战并拆除监听；失败不开战并拆除监听。`Destroy`/`ShutDown` 时拆除监听。

### 2. 不开玩法场景引导

- **选择**：开战只由游戏核心在「已在目标场景」或「加载完成」时调用 `StartMatch`。
- **理由**：正式路径总是 `MainScene` 按钮；引导是第二条开战钩子，易与核心重复。
- **备选**：场景 `Start` 里开战 — 已否决。

从编辑器单独运行 `GameScene` 且未走游戏核心入口时，MUST NOT 自动开战。

### 3. 主菜单只转发

```text
UI_LocalPlay  → GameCore.StartLocalPlay()
UI_MultiPlay  → GameCore.StartMultiPlay()   （本阶段空）
UI_Exit       → GameCore.QuitGame()
```

删除面板上的场景名常量。预制体 UnityEvent 方法名可不变。

### 4. 删除战局上的切图门面

移除 `BattleManager.StartLocalPlay` 及场景监听。公开合同回到建房、进房、开战、结束、解散与离开。

## Risks / Trade-offs

- [游戏核心会变厚] → 只放入口编排；名册与 Spawn 仍不进 `GameCore`。若入口继续膨胀，再拆子系统，本变更不做。
- [场景监听挂在核心] → 用 pending 标志 + 具名回调，加载结束或销毁时拆除，避免误把无关的同名场景加载当成开战。
- [加载条 / 失败 UI] → 仍由现有 `SceneLoader` 负责。

## Migration Plan

1. 在 `GameCore` 实现单机入口与联机空方法；接场景完成/失败。
2. 面板改为只调核心；删战局 `StartLocalPlay`。
3. 不改 `GameScene` 引导。
4. 编译、EditMode 房间测试、主菜单单机走查。
