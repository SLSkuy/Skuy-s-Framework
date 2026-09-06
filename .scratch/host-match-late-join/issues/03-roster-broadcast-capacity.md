# 03: 第二人入座与满员

**What to build:** 对局进行中再来一名加入方时，先到的加入方也能看到更新后的完整名册。房间满 4 人后下一次加入被拒绝，被拒方没有房间、回到菜单。

**Blocked by:** 02 加入方中途入座

**Status:** resolved

- [x] 第二名加入方被接受后，房主与一名加入方的名册均为同一份完整名单
- [x] 入座后广播的是完整名册，而不是只通知新人
- [x] 第 5 人加入被拒绝，该进程无活动房间、不在对局、回到菜单
- [x] 满员拒绝不影响已在席四人的对局与名册
- [x] 测试覆盖「先到者在后到者入座后名册变长」以及「满员拒绝后无房间」

## Answer

入座成功后，加入响应带完整 `playerIds`；已入座加入方再收到同一份名单会覆盖本机名册。满 4 人后 `Admit` 失败，被拒方不建房、不提交对局；`JoinSettled(false)` 仍走既有回菜单拆会话路径。

## Comments

- 接缝：战局管理器 `HandleGameJoinRequest` / `HandleGameJoinResponse`（完整名册在 `Game_Join_Response.PlayerIds`）。
- 房主侧 `BattleServerHandler` 在接受后 `BroadcastReliable` 同一份加入响应，先到者走「已有房间则 ApplyRoster」。
- EditMode：`SecondJoiner_FirstJoinerRosterGrowsToFullList`、`FifthJoin_IsRejectedWithoutChangingSeatedRosterOrMatch`。回菜单覆盖沿用 02 的拒绝/`HandleJoinFailed` PlayMode。
