# 对局态登记粘合点，本机 pawn 晚于关卡完成

菜单意图只建名册。进入对局流程态才登记 `GameManager`；粘合点自己向名册读会话角色，切关并启核。Procedure 不调用 `InitMatch` / `EnterMatch`。关卡 `SceneLoadEvent.Completed` 之后，粘合点再通知 `LocalPawnModule`：仅房主与本机玩用 `HostPlayerId` 经实例门面生成 `NetPlayer`，登记模拟核并附身，出生在原点。加入方本刀不 spawn。启核或切关失败由粘合点发出编排失败，Procedure 作为门闩 `LeaveSession`。

**Considered Options**: Procedure 点名 `EnterMatch`（门闩兼玩法编排）；pawn 放进模拟核或关卡控制；等关卡就绪再开钟（第二道开战门闩）；每个进程生成本机 pawn / 按名册全员 spawn。

**Consequences**: 对局与时钟可以早于 pawn；`GameplayPhase.InLevel` 跟关卡 Completed，不跟开钟、不跟附身。切关时实例门面先清空，必须在 Completed 之后实例化。`HostSimulationKernel` 不再负责生成与附身。加入方进关卡仍是空世界，直到复制另刀。
