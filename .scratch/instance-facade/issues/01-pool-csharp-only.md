# 01: 对象池只留纯 C# 池

**What to build:** 对外不再能用对象池子系统按资源位置取还 GameObject。Timer 等纯 C# 池照旧登记、取还。预制体实例化不再假装走对象池。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] 按资源位置 Get/Release/Prewarm 预制体的对外入口已不存在
- [ ] 对象池子系统不再向资源门面要实例
- [ ] Timer 仍能从对象池取还纯 C# 对象
- [ ] 进程仍登记对象池子系统
