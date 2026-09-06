# 05: 旁路不再当会话主人

**What to build:** 传输沙盒仍可单独测连接与收发，但不得登记战局管理器、不得充当会话主人。旧的本地同步测试场景不再作为可玩入口，避免再走「没房间也能玩」的路径。

**Blocked by:** 01 菜单开战进关（本机房与房主）

**Status:** resolved

- [x] 传输测试场景启动后没有活动房间、没有战局管理器会话
- [x] 在传输测试里启停服务端/客户端不会创建房间或开战
- [x] 旧同步测试场景不能从正式入口或构建可玩路径进入对局
- [x] 正式菜单的本机玩/开多人/加入仍只走流程核心意图，不经过传输沙盒

## Answer

`SyncTest` 与 `TransportTest` 已从构建可玩场景中关闭，只留在工程里给编辑器手测。传输面板只登记 `NetServer`/`NetClient`，不持有也不登记战局管理器。正式菜单仍只调 `ProcedureCore` 的本机玩/开多人/加入。

## Comments

- `NetworkTestPanel` 去掉已注释的运行时自动创建，避免再当入口。
- EditMode：`PlayableBuild_ExcludesLegacySyncAndTransportSandbox`、`NetworkTestPanel_DoesNotHoldBattleManager`。菜单意图覆盖沿用 01 的 `MainScenePanel` → `ProcedureCore`。
