Status: ready-for-agent

# 实例门面

Respect: `CONTEXT.md`, `docs/adr/0003-resource-facade-vs-instance-facade.md`.

## Problem Statement

资源门面已经只做加载与卸载，预制体实例化从进程入口上消失了。玩法、对象池和快捷方式仍需要从资源位置生成 GameObject，却既不能把实例化塞回资源门面，也不能继续对外暴露一套预制体对象池。调用方如果自己拿资源句柄 Instantiate 或 Dispose，句柄与场景里的实例会对不齐；切场景若直接卸光资源，池里的实例会变成空壳。

## Solution

增加进程级实例门面：调用方只按资源位置 Instantiate / InstantiateAsync / Release。内部向资源门面加载，再用 Yoo 的实例化 API 生成，并按资源位置池化。预制体句柄由实例门面持有；借出为零超过全局空闲超时，或切场景清空时，才销毁空闲实例并释放句柄。资源门面保持只加载。纯 C# 池仍由现有对象池子系统对外。UI 根与相机改接线不在本切片。

## User Stories

1. As a 玩法系统, I want 用资源位置同步生成一个 GameObject, so that 我不必先 Load 再自己 Instantiate。
2. As a 玩法系统, I want 生成时指定父节点、位置和旋转, so that 实例直接出现在世界里该在的地方。
3. As a 玩法系统, I want 用资源位置异步生成 GameObject, so that 首次加载预制体时不会卡在同步路径上。
4. As a 玩法系统, I want InstantiateAsync 在池里已有空闲实例时当帧拿到对象, so that 命中不必再等一帧。
5. As a 玩法系统, I want 同一资源位置第二次生成尽量复用已还池的实例, so that 不会每个子弹都重新 Instantiate。
6. As a 玩法系统, I want Release 一个由实例门面生成的对象后它从世界上消失但还能再被借出, so that 回收是否还池由门面决定，而不是我 Destroy。
7. As a 玩法系统, I want 还池后的对象处于未激活并离开原父节点, so that 不会继续留在关卡层级里被看见或被场景卸掉时连带搞乱。
8. As a 玩法系统, I want 不必知道内部池的存在, so that 我不会去 Get/Prewarm 一套第二对象池 API。
9. As a 玩法系统, I want 不要预热 API, so that 池的填充只发生在真正 Instantiate 之后。
10. As a 玩法系统, I want 资源位置无效或加载失败时得到空引用并看到错误日志, so that 缺资源是预期失败，我能处理空，而不是门面假装成功。
11. As a 玩法系统, I want 误 Release 不是实例门面生成的对象时被错误日志拒绝且对象不被乱 Destroy, so that 场景物体不会被门面误杀。
12. As a 玩法系统, I want 已借出的实例在我还没 Release 之前一直可用, so that 空闲超时不会把场上正在用的对象抽掉。
13. As a 玩法系统, I want 某一资源位置全部还池之后开始空闲计时, so that 暂时不用的预制体最终会卸掉句柄。
14. As a 玩法系统, I want 空闲计时期间再次 Instantiate 同一资源位置时计时清零, so that 刚还池又立刻要用时不会误卸。
15. As a 玩法系统, I want 借出为零并空闲满 30 秒后该资源位置的空闲实例被销毁、预制体句柄被释放, so that 内存会回来。
16. As a 玩法系统, I want 超时卸掉之后再 Instantiate 同一资源位置仍能生成, so that 卸句柄不是永久禁掉这个位置。
17. As a 玩法系统, I want 所有预制体共用同一个 30 秒超时, so that 我不必给每个资源位置配参数。
18. As a 玩法系统, I want 通过进程快捷方式 Instantiate / InstantiateAsync / Release, so that 不必到处 Get 实例门面类型。
19. As a 玩法系统, I want Load / LoadAsync 仍然只加载资源、不生成实例, so that 数据资产和预制体生成不会混成一个快捷方式。
20. As a 开发者, I want 实例门面登记为进程子系统且优先级在资源门面之后, so that 生成时资源门面已经存在。
21. As a 开发者, I want 实例门面只向资源门面要资源句柄, so that 它不直接找 Yoo 包、不复制一套加载。
22. As a 开发者, I want 生成使用 Yoo 资源句柄上的同步/异步 Instantiate, so that 不自己 Object.Instantiate 已加载的预制体绕开策略。
23. As a 开发者, I want 资源门面继续返回未封装的资源句柄, so that 词表里的资源句柄仍是 Yoo 的句柄。
24. As a 开发者, I want 约定业务不对该句柄 Instantiate 或 Dispose 预制体句柄, so that 本切片不靠换类型去禁止旁路。
25. As a 开发者, I want 预制体句柄由实例门面持有直到空闲超时或清空, so that 调用方不会手动 Dispose 把池抽空。
26. As a 开发者, I want 资源门面仍然不实例化、不持有预制体池, so that ADR-0003 的拆分不被代码推翻。
27. As a 关卡控制, I want 切场景完成时先让实例门面清空自己的实例和句柄, so that 随后资源门面卸载时不会留下活着的预制体引用。
28. As a 关卡控制, I want 清空实例门面之后再让资源门面卸光, so that 顺序不会反。
29. As a 关卡控制, I want 场景 IO 仍走现有场景加载器而不是资源门面, so that 关卡加载与预制体生成分开。
30. As a 关卡控制, I want 实例门面清空时销毁所有借出与空闲实例, so that 切到下一关不会带走上一关的池。
31. As a 进程, I want 实例门面销毁时同样清空池并释放句柄, so that 退出进程不留实例。
32. As a 使用纯 C# 池的子系统, I want 仍能向对象池子系统登记/取还无 GameObject 的对象, so that Timer 一类路径不被预制体池重构牵连。
33. As a 开发者, I want 对象池子系统去掉对外的预制体 Get/Release/Prewarm, so that 预制体池只存在于实例门面内部。
34. As a 开发者, I want 对象池子系统不再依赖资源门面去 Instantiate, so that 那条已注释的死路径被删掉而不是留着。
35. As a 开发者, I want 没有第二套「资源管理系统」这个名字, so that 文档和类型都只用资源门面与实例门面。
36. As a 开发者, I want 本切片不改 UI 根的生成方式, so that 实例门面骨架可以单独合入。
37. As a 开发者, I want 本切片不改相机如何找到预制体, so that 相机改走资源位置可以另开。
38. As a 测试, I want 只通过实例门面的对外生成/回收/清空/超时行为断言对错, so that 不必打开内部字典或去 spy 资源门面调用次序。
39. As a 测试, I want 用测试资源策略注入资源门面来提供可实例化的预制体, so that 不必在测试里启动完整热更管线。
40. As a 测试, I want 推进实例门面的帧更新来验证 30 秒空闲卸句柄, so that 超时是可观测行为而不是睡真实半分钟的手动步骤（测试可将时间加速或连续喂 deltaTime）。
41. As a 测试, I want 需要加载的 InstantiateAsync 的完成发生在至少一帧之后, so that 首次加载的时序可观测；池命中当帧完成。
42. As a 测试, I want 同一资源位置还池后再同步 Instantiate 拿到的是可用实例, so that 池命中不必再走失败的加载。
43. As a 调用方, I want 空资源位置（空字符串）被当成失败并打日志返回空, so that 与缺资源同一类预期失败。
44. As a 调用方, I want 对已 Release 的实例再次 Release 被当成外源/非法并打日志忽略, so that 双 Release 不会把池结构打乱。
45. As a 调用方, I want 资源门面尚未注入策略时的失败保持资源门面现有不变量（视为初始化错误而不是缺资源）, so that 不会在实例门面里吞掉「没注入」。
46. As a 开发者, I want 实例门面的空闲计时走子系统 Update, so that 不另接一套计时器子系统。
47. As a 开发者, I want 多个不同资源位置的池互不影响, so that 卸掉 A 的句柄不会 Recycle 掉 B 的借出。
48. As a 玩法系统, I want 借出期间对象保持激活（除非我自己关掉）, so that 从池里拿出来就可以用。
49. As a 开发者, I want 进程快捷方式的生成失败与门面失败一致（空引用 + 错误日志）, so that Global 不是另一套语义。
50. As a 开发者, I want 本切片不把 ScriptableObject 单例的 Resources.Load 旁路改掉, so that 范围停在实例门面与预制体池拆分。

## Implementation Decisions

- 遵守 ADR-0003 与词表：资源门面只加载/卸载；实例门面只从资源位置生成/回收；禁止用「资源管理系统」指其中任何一者。
- 新增进程子系统，类型名为 InstantiationManager，词表称实例门面。优先级数值介于资源门面与对象池子系统之间。进程壳在资源门面之后登记它。
- 对外 API：按资源位置同步 Instantiate、异步 InstantiateAsync、按实例 Release。支持父节点与位姿。不提供预热、不提供按资源位置的对外清池、不暴露内部队列。切场景与进程销毁走门面上的清空（销毁全部借出与空闲实例并释放其预制体句柄）。
- 进程快捷方式：Instantiate / InstantiateAsync / Release 转发实例门面；Load / LoadAsync 仍只转发资源门面。
- 加载路径：实例门面只通过资源门面 Load/LoadAsync 取得资源句柄，再调用该句柄的 Yoo Instantiate 同步/异步 API。不直接持有 Yoo 包。资源策略仍不负责实例化。
- 内部按资源位置一份池。池未命中才加载并 Instantiate；命中则取出空闲实例、激活并放到请求的父节点/位姿。Release 将实例失活、挂回门面自己的池根、记入空闲。调用方不 Dispose 预制体句柄。
- 资源句柄保持 Yoo 原类型、门面不二次封装。业务不直接对该句柄 Instantiate/Dispose 只靠约定与本切片的正确入口，不靠换返回类型强制。
- 借出计数：某资源位置借出 > 0 时不计空闲超时。借出变为 0 后用子系统 Update 累加时间；期间再次借出则清零。全局唯一超时为 30 秒（所有资源位置相同）。超时：销毁该位置空闲实例并 Dispose 对应预制体句柄。之后再生成视为首次加载。
- InstantiateAsync：池命中当帧把实例交给调用方；需要加载时至少下一帧完成（测试用喂 deltaTime/等一帧观察加载路径，不测内部调度器类型）。
- 预期失败（空资源位置、加载失败、无法实例化）：错误日志 + 返回 null。资源门面未注入策略等初始化不变量仍由资源门面按现有方式失败，实例门面不得改成「缺资源」。
- Release 外源对象、空引用、或不在借出表中的对象：错误日志并忽略，不 Destroy。
- 切场景：场景加载器在完成加载、调用资源门面卸光之前，先清空实例门面。不得只 ClearAll 资源而留下实例门面的句柄与池。
- 对象池子系统：删除预制体实例池对外 API 及其对资源门面 Instantiate 的残线；保留纯 C# 池并继续登记为子系统。
- 本切片不接线 UI 根生成、不改相机从 SO 索引场景对象、不把内存中已有 GameObject 的克隆收进实例门面。
- 资源策略如何在进程启动时注入，沿用资源门面既有契约。本切片可用测试策略注入；不把热更、清单、多包下载做成实例门面的工作。

## Testing Decisions

- 只测对外行为：给定资源位置，Instantiate / InstantiateAsync 是否得到可用实例、Release 后能否再借出、借出未还时超时不得卸掉、还清后满 30 秒不得再拿到旧空闲实例（应重新生成或等价于句柄已释放后的新生命周期）、切场景清空后旧实例不可用、失败返回空且不抛成「缺资源以外的初始化异常」、需要加载的 InstantiateAsync 至少一帧后完成、池命中当帧完成。不测私有字典、不测是否调用了某一句 Yoo API 名字、不 spy 资源门面方法次序。
- **接缝（仅此一条）：实例门面的对外 API（含其 Update 所表现的空闲超时，以及供场景加载器调用的清空）。** 资源门面用已有策略注入点配测试替身，以便提供可实例化的预制体；断言仍打在实例门面上，不把资源门面变成第二条接缝。
- 仓库几乎没有对等的资源 EditMode 范本。新测试放在测试程序集，面向实例门面。若 EditMode 无法构造可用的 Yoo 资源句柄，允许 PlayMode 夹具预制体，但接缝高度不变。
- 超时测试通过连续向子系统 Update 喂时间，不要 `WaitForSeconds(30)` 作为主路径。

## Out of Scope

- UI 根、面板/窗口按资源位置打开。
- 相机改为资源位置 + 实例门面。
- 封装或替换资源句柄类型；禁止旁路的静态分析。
- 热更、下载、多 package 路由、场景经资源门面加载。
- 对外预热、按位置配置不同超时、把纯 C# 池并进实例门面。
- 扫掉工程里所有 `Object.Instantiate`（含对内存中已有对象的克隆）。

## Further Notes

- 语言：实例门面、资源门面、资源策略、资源位置、资源句柄。Avoid：资源管理系统、资源门面兼做实例化、用 PoolManager 指预制体池。
- 当前资源门面在进程壳里可能尚未注入策略；那是资源门面启动问题。实例门面不得为此再实现一套加载。测试注入替身即可验证本切片。
