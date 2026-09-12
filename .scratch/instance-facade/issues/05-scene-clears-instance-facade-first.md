# 05: 切场景先清实例门面再卸资源

**What to build:** 关卡场景加载完成后，先清空实例门面（所有借出与空闲实例销毁、预制体句柄释放），再让资源门面卸光。场景 IO 仍走场景加载器，不经资源门面加载场景。

**Blocked by:** 02: 同步实例门面（内部池与快捷方式）

**Status:** ready-for-agent

- [x] 场景加载成功完成时，实例门面先于资源门面卸光被清空
- [x] 清空后此前生成的实例不可再当活对象使用，再 Instantiate 视为新生命周期
- [x] 不得只卸资源而留下实例门面的池与句柄
- [x] 测试仍打在实例门面清空与之后资源已卸的对外结果上，不测场景 YAML

## Comments

- `SceneLoader` 在场景加载成功完成时先 `InstantiationManager.Clear()`，再 `ResourceManager.ClearAll()`。失败路径不清实例门面。场景 IO 仍是 `SceneManager.LoadSceneAsync`。
- 测试覆盖「先清实例门面再卸资源」之后旧实例不可用、再生成是新生命周期；不加载场景 YAML、不 spy 调用次序。
