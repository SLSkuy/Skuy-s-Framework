# 身份、同步根、控制器收敛设计

## 背景

当前网络实体由 `NetEntityIdentity` 保存身份和角色，由 `NetEntitySyncRoot` 扫描同步模块并分发角色，由不同 Controller 负责本地预测、服务端权威模拟或远端插值。

下一步目标是进一步收敛入口：实体只需要挂载一个网络同步根组件，即可完成身份标识、同步模块扫描、角色应用、Controller 自动装配和后续同步调度。

因此，`NetEntityIdentity` 不再作为独立必挂组件继续存在，而是并入 `NetEntitySyncRoot` 的职责边界。

## 核心结论

`NetEntitySyncRoot` 应成为网络实体的唯一入口组件。

它需要统一负责：

- 保存 `EntityId`。
- 保存并切换 `NetEntityRole`。
- 暴露 `RoleChanged`。
- 扫描并注册 `INetSyncComponent`。
- 对同步模块分发 `ConfigureRole(...)`。
- 根据当前 `NetEntityRole` 自动添加或启用对应 Controller。
- 为 Controller 提供同步模块查询入口。
- 驱动需要每帧更新的同步模块。

`NetEntityIdentity` 的能力可以直接迁移到 `NetEntitySyncRoot`，迁移完成后不再要求实体额外挂载 `NetEntityIdentity`。

## 目标结构

```text
Entity GameObject
  -> BaseEntity / EntityCharacter
  -> NetEntitySyncRoot
      -> EntityId / Role
      -> Sync Module Registry
      -> Controller Binding
      -> Role Dispatch
  -> NetPositionSync
  -> NetAnimationSync
  -> Other Net Sync Modules
  -> Role Controller, runtime added
```

Controller 不再是 prefab 上必须预挂的固定组件，而是由 `NetEntitySyncRoot` 根据角色创建或切换。

## 角色到 Controller 的映射

建议先使用固定映射，后续再做 ScriptableObject 配置化。

| `NetEntityRole` | Controller | 驱动模式 | 说明 |
| --- | --- | --- | --- |
| `LocalPlay` | `PlayerController` | `LocalInput` | 单机或本地非网络测试输入 |
| `Predict` | `PlayerController` | `Prediction` | 客户端拥有者，采集输入、预测、接收权威校正 |
| `Authority` | `AuthorityController` | `Authority` | 服务端权威模拟，消费输入并产出快照 |
| `Replica` | `ReplicaController` | `Replica` | 非拥有者远端实体，消费权威快照并插值 |

`AIController` 暂时不直接由 `NetEntityRole` 决定。AI 可以作为实体控制来源的一种，后续通过 `EntityDriveMode.AI` 或独立 `EntityControlProfile` 接入。

## Controller 生命周期

`NetEntitySyncRoot` 需要维护当前 Controller：

```text
private EntityControllerBase _activeController;
```

当角色初始化或变化时：

1. 刷新同步模块。
2. 对所有同步模块执行 `ConfigureRole(role)`。
3. 根据 `role` 找到目标 Controller 类型。
4. 如果当前 Controller 类型不匹配，则解绑旧 Controller。
5. 获取或添加目标 Controller。
6. 将 `BaseEntity`、`NetEntitySyncRoot`、输入源等上下文注入 Controller。
7. 更新实体 Tick 驱动策略。

如果实体已经挂有对应 Controller，可以复用；如果没有，则运行时 `AddComponent`。

不建议在切换角色时直接销毁旧 Controller。第一阶段可以先禁用旧 Controller 或只保留一个活动 Controller，避免在调试期丢失 Inspector 状态。等流程稳定后再决定是否清理多余组件。

## 初始化入口

`NetEntitySyncRoot` 应提供原 `NetEntityIdentity` 的初始化能力：

```text
Init(uint entityId, NetEntityRole role)
SetRole(NetEntityRole role)
```

并提供原身份查询属性：

```text
EntityId
Role
IsInitialized
IsAuthority
IsPredictingOwner
IsReplica
IsLocalPlay
```

这样外部系统只依赖 `NetEntitySyncRoot`，不再需要知道 `NetEntityIdentity`。

## 对现有类型的调整方向

### `NetEntityIdentity`

迁移方向：

- 将 `entityId`、`role`、`RoleChanged`、`Init(...)`、`SetRole(...)` 移动到 `NetEntitySyncRoot`。
- 所有外部引用改为依赖 `NetEntitySyncRoot`。
- 第一阶段可以暂时保留 `NetEntityIdentity` 文件但不再作为运行时入口；最终删除该组件，避免双身份源。

完成标准：

- prefab 只需要挂 `NetEntitySyncRoot`。
- 不再存在 `[RequireComponent(typeof(NetEntityIdentity))]`。
- 同一个实体上只存在一个权威身份状态来源。

### `NetEntitySyncRoot`

新增职责：

- 身份状态管理。
- Controller 自动装配。
- Controller 当前实例查询。

保留职责：

- 同步模块扫描。
- 同步模块注册。
- 同步模块查询。
- 角色分发。
- 同步模块更新。

需要避免：

- 不把具体移动、动画、技能逻辑写进 Root。
- Root 只负责装配和调度，不负责实现某个同步模块的业务细节。

### `BaseEntity`

当前 `BaseEntity` 缓存的是 `NetEntityIdentity`。

迁移后应改为缓存 `NetEntitySyncRoot`，并从 Root 读取 `EntityId` 和初始化状态。

完成标准：

- `BaseEntity.EntityId` 来源为 `NetEntitySyncRoot.EntityId`。
- `BaseEntity.HasIdentity` 来源为 `NetEntitySyncRoot.IsInitialized`。
- `BaseEntity` 不再直接认识 `NetEntityIdentity`。

### `NetPositionSync`

当前 `NetPositionSync` 仍依赖 `NetEntityIdentity`。

迁移后应依赖 `NetEntitySyncRoot`：

- 从 Root 读取 `EntityId`。
- 从 Root 读取 `Role`。
- 仍作为普通同步模块被 Root 扫描和配置。

完成标准：

- `NetPositionSync` 不再 `[RequireComponent(typeof(NetEntityIdentity))]`。
- `NetPositionSnapshot.EntityId` 来自 `NetEntitySyncRoot.EntityId`。

### `PlayerController`

`PlayerController` 已经开始依赖 `NetEntitySyncRoot` 查询 `NetPositionSync`。

下一步应由 Root 自动添加并初始化：

- `LocalPlay` 时初始化为本地输入控制。
- `Predict` 时初始化为预测控制。
- 输入源由 Root 或外部实体创建流程传入，第一阶段可以继续使用 `LocalInputProvider` 作为默认实现。

需要保留：

- 输入采集。
- 本地预测。
- 权威校正和回放。

需要移出：

- 测试性质的 `Start()` 自动初始化和相机绑定，后续应迁移到场景启动或玩家生成流程。

### `AuthorityController`

由 Root 在 `Authority` 角色下自动添加或启用。

初始化参数应收敛为：

```text
Init(BaseEntity entity, NetEntitySyncRoot syncRoot)
```

完成标准：

- 不需要外部手动提前挂载。
- 不需要外部重复传入 `EntityCharacter` 专用类型，优先依赖 `BaseEntity`。

### `ReplicaController`

由 Root 在 `Replica` 角色下自动添加或启用。

迁移后不再持有 `NetEntityIdentity`，改为持有 `NetEntitySyncRoot`。

完成标准：

- 快照实体校验通过 `syncRoot.EntityId`。
- 角色校验通过 `syncRoot.Role` 或 `syncRoot.IsReplica`。

## 推荐第一阶段实施顺序

1. 在 `NetEntitySyncRoot` 内复制 `NetEntityIdentity` 的身份字段、属性、事件和初始化 API。
2. 将 `BaseEntity`、`NetPositionSync`、`ReplicaController` 的身份依赖改为 `NetEntitySyncRoot`。
3. 移除各类 `[RequireComponent(typeof(NetEntityIdentity))]`。
4. 在 `NetEntitySyncRoot.ApplyRole(...)` 后增加 Controller 装配流程。
5. 让 `PlayerController`、`AuthorityController`、`ReplicaController` 的 `Init(...)` 统一接收 `BaseEntity` 与 `NetEntitySyncRoot`。
6. 确认 prefab 只挂 `NetEntitySyncRoot` 就能完成角色驱动和同步模块发现。
7. 最后删除或废弃 `NetEntityIdentity`，避免双入口长期存在。

## Controller 自动装配的边界

第一阶段只做角色到 Controller 的直接映射，不引入复杂配置。

可以暂时硬编码：

```text
LocalPlay -> PlayerController
Predict -> PlayerController
Authority -> AuthorityController
Replica -> ReplicaController
```

但要把映射逻辑集中在 `NetEntitySyncRoot` 的一个私有方法中，方便后续替换为配置：

```text
ResolveControllerType(NetEntityRole role)
```

后续当实体种类变多时，可以扩展为：

```text
EntityControllerProfile
  EntityKind
  Role
  ControllerType
  RequiredModules
```

## 与技能系统的衔接

本轮不实现技能系统，但 Root 的职责划分需要为技能系统留出接口。

建议后续技能系统仍然作为实体能力模块存在：

```text
BaseEntity / EntityCharacter
  -> SkillOwner / SkillCaster
  -> SkillController
  -> NetSkillSync
```

`NetEntitySyncRoot` 不直接执行技能，只负责：

- 识别 `NetSkillSync`。
- 按角色配置技能同步模块。
- 为 Controller 提供同步模块查询入口。

技能释放、冷却、命中、动画事件仍由技能系统内部处理。

## 与网络同步系统的衔接

迁移后，外部网络系统处理快照时不应再直接查找 `NetEntityIdentity`。

推荐流程：

```text
Network Message
  -> Entity Registry by EntityId
  -> NetEntitySyncRoot
  -> Target Sync Module / Active Controller
```

其中：

- 输入消息交给 `AuthorityController`。
- 权威快照交给 `PlayerController` 或 `ReplicaController`。
- 普通同步模块快照交给对应 `INetSyncSnapshotReceiver<T>`。

Root 是实体内部入口，Entity Registry 是实体外部查找入口。

## 风险点

- `NetEntityIdentity` 与 `NetEntitySyncRoot` 并存期间可能出现双身份源。
- Controller 自动添加后，原先 prefab 上预挂 Controller 的测试流程可能重复初始化。
- `PlayerController.Start()` 内仍有测试初始化逻辑，自动装配后需要清理。
- `BaseEntity` 迁移到 Root 后，要保证非网络实体也能通过 `LocalPlay` 或未初始化状态运行。
- `RequireComponent` 只能保证编辑器和 AddComponent 时的依赖，不适合表达“运行时按角色创建 Controller”的目标。

## 完成标准

完成本阶段后应满足：

- 一个网络实体 prefab 只需要显式挂载 `NetEntitySyncRoot`。
- `NetEntitySyncRoot` 同时承担身份、角色、同步模块注册、Controller 装配职责。
- 不再需要独立 `NetEntityIdentity` 作为运行时组件。
- Controller 不再由 prefab 固定预挂，而是根据 `NetEntityRole` 自动添加或启用。
- 同步模块仍保持模块化，新增同步能力只需要新增对应 `INetSyncComponent`。
- Root 不包含具体同步逻辑，只负责发现、注册、查询、配置和调度。
