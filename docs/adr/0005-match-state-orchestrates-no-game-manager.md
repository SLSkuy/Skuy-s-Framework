# 对局态直接编排玩法，删除粘合类型

`GameManager` 与流程核抢同一段对局寿命：一个是进程级门闩，一个是对局级子系统，菜单里必须有前者、对局里才允许后者。决定删除 `GameManager` 与 `GameplayPhase`，退役「玩法粘合点」；切关、启核、本机 pawn 由对局流程态直接登记，离开该态时拆除。名册对象仍独立。界面只对流程核发意图；HUD 留在流程核。切关或启核失败由对局态直接回到菜单，不再经 `OrchestrationFailed`。本机 pawn 仍晚于关卡完成（ADR-0004 未废止的部分）。

**Status**: accepted；取代 ADR-0004 中「对局态登记 `GameManager`」；ADR-0002 的名册独立、流程只进出对局仍有效，其中「另建 `GameManager` 做玩法编排」不再成立。

**Considered Options**: 把 `GameManager` 升成进程级单例兼 FSM（菜单里也叫粘合点）；名册并进同一控制器（ADR-0002 已否决）；对局态再抽不登记到 Global 的第二层编排器（会再长回粘合类型）。

**Consequences**: 对局流程态会变厚。UGF/StarForce 把切关写在 Procedure 的 OnEnter/OnLeave，不另设 GameManager。没有第二套玩家可观察流程态；有没有关卡问关卡控制或当前场景。
