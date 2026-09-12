# 资源门面与实例门面拆分

加载与生成拆成两个进程级入口：资源门面只加载/卸载并转发资源句柄；实例门面从资源位置生成与回收 GameObject，内部池化，经资源门面取句柄后再用 Yoo 的 Instantiate。预制体实例池不对外、不放在 PoolManager。不封装资源句柄来禁止 Instantiate，只靠约定；预制体句柄由实例门面在「借出为零且超过全局空闲超时」或切场景清空时释放。切场景先清实例门面，再让资源门面卸载。

**Considered Options**: 一个门面兼做加载和实例化；封装句柄去掉 Instantiate；保留对外的 prefab PoolManager；调用方手动 Dispose 句柄。

**Consequences**: 类型名为 `InstantiationManager`。全局空闲超时 30 秒。`InstantiateAsync` 池命中当帧完成，需要加载时至少下一帧完成。`Global` 的 Load 与 Instantiate 分属两扇门面。纯 C# 池仍由 PoolManager 对外。不提供预热。UI 根与 CameraManager 改接线不在本决策的实现切片里。
