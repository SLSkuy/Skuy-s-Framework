# 03: InstantiateAsync 加载路径至少下一帧完成

**What to build:** 按资源位置异步生成时，池命中当帧交出实例；需要加载时调用方等到至少下一帧才拿到对象。失败语义与同步路径相同。

**Blocked by:** 02: 同步实例门面（内部池与快捷方式）

**Status:** ready-for-agent

- [x] InstantiateAsync 在需要加载时最终能生成可用实例（或失败返回空并打日志）
- [x] 池命中时 InstantiateAsync 当帧交出实例
- [x] Global.InstantiateAsync 与门面行为一致
- [x] 测试只断言对外完成时机与实例可用性，不测内部调度器类型

## Comments

- `InstantiateAsync` 返回 `Task<GameObject>`。池命中：当帧完成并激活。需要加载或失败：下一次子系统 `Update` 才交出实例或空 + 错误日志。
- 未注入资源策略时仍立刻抛初始化异常，与同步路径相同。`Global.InstantiateAsync` 转发实例门面。
- 2026-09-12：改为池命中不再延迟（原「无论是否命中都下一帧」已废）。
