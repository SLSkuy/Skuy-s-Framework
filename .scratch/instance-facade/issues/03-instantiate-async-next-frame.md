# 03: InstantiateAsync 至少下一帧完成

**What to build:** 按资源位置异步生成时，无论池里有没有空闲实例，调用方都要等到至少下一帧才拿到对象。失败语义与同步路径相同。

**Blocked by:** 02: 同步实例门面（内部池与快捷方式）

**Status:** ready-for-agent

- [ ] InstantiateAsync 在需要加载时最终能生成可用实例（或失败返回空并打日志）
- [ ] 池命中时 InstantiateAsync 也不在同一帧交出实例
- [ ] Global.InstantiateAsync 与门面行为一致
- [ ] 测试只断言对外完成时机与实例可用性，不测内部调度器类型
