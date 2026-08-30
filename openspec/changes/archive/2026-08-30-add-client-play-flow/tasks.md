## 1. 游戏核心玩法入口

- [x] 1.1 在 `GameCore` 增加单机入口（如 `StartLocalPlay`）：经 `BattleMgr` 确保本机房间与进房；已在约定玩法场景则 `StartMatch`，否则用具名方法监听场景完成/失败后 `LoadScene`，成功则开战、失败则不开战，结束或销毁时拆除监听。场景名由 `GameCore` 持有。对照 `design.md` Decision 1 验收。
- [x] 1.2 在 `GameCore` 增加与单机对称的联机入口（如 `StartMultiPlay`），本阶段空实现。确认方法可被主菜单调用且不拉起网络。

## 2. 战局去掉切图

- [x] 2.1 从 `BattleManager` 删除 `StartLocalPlay`、pending 场景字段及 `SceneLoadEvent` / `SceneManager` 依赖，公开合同仅保留建房、进房、开战、结束、解散与离开。全文搜索确认无战局侧 `StartLocalPlay`。
- [x] 2.2 运行 `Tests.Editor.BattleRoomEditModeTests`，确认仍为全部通过。

## 3. 主菜单只转发意图

- [x] 3.1 将 `UI_LocalPlay` 改为只调 `GameCore` 单机入口，`UI_MultiPlay` 只调联机入口，`UI_Exit` 保持 `QuitGame`；删除面板上的玩法场景名常量。对照脚本确认面板无建房、无 `LoadScene`、无开战。

## 4. 集成核对

- [x] 4.1 确认未新增 `PlayFlow` 类型、未在 `GameScene` 增加开战引导组件。等待编译结束，Console 无新增错误；EditMode 房间测试仍通过。
- [x] 4.2 从 `MainScene` 点单机：加载玩法场景后存在 LocalPlay 角色且可操作；主菜单销毁后仍能开战。记录无法在本环境 PlayMode 验证的部分。
