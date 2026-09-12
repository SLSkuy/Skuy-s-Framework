# 01: 对象池只留纯 C# 池

**What to build:** 对外不再能用对象池子系统按资源位置取还 GameObject。Timer 等纯 C# 池照旧登记、取还。预制体实例化不再假装走对象池。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] 按资源位置 Get/Release/Prewarm 预制体的对外入口已不存在
- [x] 对象池子系统不再向资源门面要实例
- [x] Timer 仍能从对象池取还纯 C# 对象
- [x] 进程仍登记对象池子系统

## Comments

- 已删除 `Get(string)` / `Get<T>(string)` / `Release(GameObject)` / `Prewarm` / `ClearPool(string)` / 按前缀清预制体池，以及 `Init` 里对资源门面和池根 GameObject 的依赖。纯 C# `RegisterPool` / `Get<T>` / `Release<T>` 保留。`GameCore` 仍登记 `PoolManager`；`TimerManager` 仍从其取还 `Timer`。
