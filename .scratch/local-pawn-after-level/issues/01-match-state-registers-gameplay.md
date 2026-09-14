# 01: 对局态才登记粘合点，登记即切关启核

**What to build:** 本机玩、开多人、加入被接受之后，玩家仍立刻进入对局；菜单意图只建名册，进入对局流程态才出现玩法粘合点。粘合点自己向名册读会话角色、请关卡控制加载关卡、启动模拟核。流程核不再点名切关或启核。对局调试 HUD 仍能看名册并离开。本票还不生成角色。

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] 本机玩：菜单意图之后、进入对局态之前没有玩法粘合点；对局态下粘合点存在，关卡控制被要求加载关卡，权威模拟核在跑
- [x] 开多人：同样在对局态才有粘合点；名册已开听，切关与启核由粘合点自己开始
- [x] 加入方被接受后进入对局态才有粘合点，关卡控制被要求加载关卡，客户端模拟核在跑；未被接受时没有粘合点
- [x] 流程核不调用粘合点的切关、启核或初始化对局入口；UI 仍只对流程核发意图
- [x] 离开或解散后粘合点注销，流程回到菜单，仍只有一份进程壳
- [x] 测试走粘合点编排表面与流程核何时登记，不测核内部步进、不测场景 YAML

## Answer

菜单意图只建名册；`ProcedureMatchState` 进入时才 `Register<GameManager>`。粘合点 `Init` 向名册读会话角色，请求关卡控制加载对局关卡并启动 Host/Client 模拟核。已删除 `InitMatch` / `EnterMatch`。EditMode 接缝：`Assets/Tests/Editor/GameplayOrchestrationTests.cs`。

## Comments

开多人开听仍由 `CreateHostRoom` 承担；EditMode 未走真实 `NetServer`/`JoinRemote`（需资源门面）。`Simulator.Init` 暂用 `SimulationConfig` 默认值以便 EditMode 启核，不测核步进。启核失败回菜单留给 02。
