# 02: 同步实例门面（内部池与快捷方式）

**What to build:** 玩法按资源位置同步 Instantiate，Release 还池后再 Instantiate 能拿到可用实例。进程快捷方式与门面语义一致。缺资源或空位置返回空并打错误日志；Release 外源对象打日志并忽略。资源门面仍只加载。测试只打在实例门面对外 API 上。

**Blocked by:** 01: 对象池只留纯 C# 池

**Status:** ready-for-agent

- [x] 进程登记实例门面，且在资源门面之后可用
- [x] Instantiate(资源位置) 经资源门面取句柄，再用 Yoo 实例化 API 生成 GameObject；可指定父节点与位姿
- [x] Release 后实例失活离开原父节点；再次 Instantiate 同一资源位置复用该实例
- [x] 不提供预热；不对外暴露内部池
- [x] Global.Instantiate / Release 与门面行为一致；Load 仍只加载不生成
- [x] 空位置或加载/实例化失败：错误日志 + null
- [x] Release 外源或重复 Release：错误日志 + 忽略，不 Destroy 场景对象
- [x] 资源门面未注入策略时的失败仍是初始化错误，不被改成「缺资源」
- [x] 清空（含子系统销毁）会销毁借出与空闲实例并释放预制体句柄
- [x] 测试只断言实例门面对外行为；资源策略用注入替身，不断言门面协作次序

## Comments

- 新增 `InstantiationManager`（优先级 -175，资源门面之后、对象池之前），`GameCore` 紧接资源门面登记。同步 Instantiate / Release / Clear；内部按资源位置池化；句柄走资源门面 Load 后再 `AssetHandle.InstantiateSync`。
- `Global.Instantiate` / `Release` 转发实例门面；`Load` 未改。
- EditMode 测试在 `Assets/Tests/Editor/InstantiationManagerTests.cs`，经 `SetProvider` 注入可实例化替身，只断言门面对外行为。InstantiateAsync 与 30 秒空闲超时不在本票。
