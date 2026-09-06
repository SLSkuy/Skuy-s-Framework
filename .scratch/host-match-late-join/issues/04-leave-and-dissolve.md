# 04: 离座与解散

**What to build:** 加入方离开（含掉线）只从名册消失，对局对其余玩家继续。房主离开或主动解散则房间结束：所有加入方回到菜单，各方无活动房间。本阶段结束对局与解散相同。

**Blocked by:** 02 加入方中途入座

**Status:** resolved

- [x] 加入方从 HUD 离开后，自己回到菜单且无房间；房主仍在对局，名册已去掉该玩家并广播完整名册
- [x] 加入方连接断开与主动离开同一套离座规则
- [x] 房主从 HUD 解散（或房主离开）后，加入方回到菜单、拆掉客户端网络、无活动房间
- [x] 解散后关卡卸掉，流程回到菜单，网络不再为该房运行
- [x] 本机房解散与多人解散一样回到无房间菜单
- [x] 测试覆盖加入方离座后名册减少但对局仍在，以及房主解散后双方均无活动房间

## Answer

加入方连接移除走 `HandleConnectionRemoved`：只从名册删除并返回剩余完整名单，对局与开听继续。房主连接移除或 `Dissolve` 会先广播 `Accepted=false` 的加入响应，加入方按解散拆房间并 `SessionEnded` 回菜单。HUD 对房主显示「解散」、对加入方显示「离开」，都只调 `LeaveSession`。

## Comments

- 接缝：`HandleConnectionRemoved`（连接移除 / 掉线）与 `HandleGameJoinResponse`（剩余名册或解散）。
- 掉线与主动离开在房主侧都是同一条 `Leave`：非房主离座，房主则解散。
- EditMode：`JoinerLeave_RosterShrinksMatchContinuesAndRemainingJoinerSeesIt`、`HostLeave_DissolvesAndJoinerClearsRoomOnDissolveMessage`、`LocalRoomDissolve_LeavesNoRoom`。关卡卸掉回菜单沿用 01 的 `LeaveSession_UnloadsLevelAndReturnsToMenu`。
