# 02: 同步实例门面（内部池与快捷方式）

**What to build:** 玩法按资源位置同步 Instantiate，Release 还池后再 Instantiate 能拿到可用实例。进程快捷方式与门面语义一致。缺资源或空位置返回空并打错误日志；Release 外源对象打日志并忽略。资源门面仍只加载。测试只打在实例门面对外 API 上。

**Blocked by:** 01: 对象池只留纯 C# 池

**Status:** ready-for-agent

- [ ] 进程登记实例门面，且在资源门面之后可用
- [ ] Instantiate(资源位置) 经资源门面取句柄，再用 Yoo 实例化 API 生成 GameObject；可指定父节点与位姿
- [ ] Release 后实例失活离开原父节点；再次 Instantiate 同一资源位置复用该实例
- [ ] 不提供预热；不对外暴露内部池
- [ ] Global.Instantiate / Release 与门面行为一致；Load 仍只加载不生成
- [ ] 空位置或加载/实例化失败：错误日志 + null
- [ ] Release 外源或重复 Release：错误日志 + 忽略，不 Destroy 场景对象
- [ ] 资源门面未注入策略时的失败仍是初始化错误，不被改成「缺资源」
- [ ] 清空（含子系统销毁）会销毁借出与空闲实例并释放预制体句柄
- [ ] 测试只断言实例门面对外行为；资源策略用注入替身，不断言门面协作次序
