# 第一级网络同步变更文档

## 目标

第一级目标是建立一个最小但稳定的网络同步入口：实体根节点只需要挂载一个统一的网络同步根组件 `NetEntitySyncRoot`，它会自动扫描当前实体及子物体上的同步组件，并按实体网络角色完成注册、查询、角色分发和同步驱动。

本阶段不实现 Inspector 勾选后自动 `AddComponent`，不重写网络协议，不处理 yaw / pitch 策略，也不实现旋转同步。

## 使用方式

实体根节点挂载：

```text
NetEntitySyncRoot
NetEntityIdentity
```

同步能力以普通组件形式挂在实体根节点或子节点上，例如：

```text
NetPositionSync
NetAnimationSync
NetHealthSync
```

运行时由 `NetEntitySyncRoot` 自动扫描所有实现 `INetSyncComponent` 的组件，并完成注册。

## 第一阶段能力范围

包含：

- 自动扫描实体及子节点上的同步组件。
- 自动注册同步组件。
- 根据 `NetEntityRole` 分发角色。
- 支持位置同步组件。
- 支持后续动画、生命值等同步组件扩展。
- 支持同步模块按角色启停。

不包含：

- Inspector 模块列表驱动自动装配。
- 旋转同步。
- 网络协议重写。
- yaw / pitch 同步策略。

## 核心结构

```text
NetEntitySyncRoot
  - 持有 NetEntityIdentity
  - 扫描 INetSyncComponent
  - 建立 SyncModuleID 到模块实例的映射
  - 角色变化时调用 ConfigureRole
  - 在 Update 中驱动需要运行的同步模块
```

`NetEntitySyncRoot` 同时承担原注册器和角色分发职责，减少 prefab 配置数量。

## 同步组件接口

```csharp
public interface INetSyncComponent
{
    SyncModuleID ModuleId { get; }
    void ConfigureRole(NetEntityRole role);
}
```

```csharp
public interface INetSyncSnapshotSource<TSnapshot>
{
    TSnapshot CaptureSnapshot(uint snapshotTick, uint lastProcessedInputTick = 0);
}
```

```csharp
public interface INetSyncSnapshotReceiver<TSnapshot>
{
    void ApplySnapshot(in TSnapshot snapshot);
}
```

```csharp
public interface INetInterpolatedSync
{
    void UpdateInterpolation(float deltaTime);
}
```

```csharp
public interface INetSyncUpdatable
{
    void SyncUpdate(float deltaTime);
}
```

## 位置同步

位置同步组件命名为：

```text
NetPositionSync
```

位置快照命名为：

```text
NetPositionSnapshot
```

模块 ID 命名为：

```text
SyncModuleID.Position
```

位置同步只保留：

- `Position`
- `Velocity`
- `MovementState`
- `LastProcessedInputTick`
- `SnapshotTick`
- `EntityId`

应用快照时只设置：

```csharp
transform.position = snapshot.Position;
```

插值时只插值：

```csharp
Vector3.Lerp(from.Position, to.Position, t);
```

## 角色行为

### `Authority`

- 采集位置快照。
- 对外广播权威状态。
- 不执行插值。

### `Predict`

- 本地输入先行模拟。
- 记录预测快照。
- 接收权威快照后进行校正。

### `Replica`

- 不采集本地输入。
- 接收权威快照。
- 执行位置插值。

### `LocalPlay`

- 不进入网络同步流程。
- 可关闭所有网络同步模块。

## 完成标准

- 实体只需要一个统一网络同步入口组件。
- 已挂载同步能力组件可以被自动扫描。
- 同步组件可以按 `SyncModuleID` 查询。
- 角色变化会自动分发到所有同步组件。
- 位置同步不处理 rotation。
- `NetPositionSync` 名称与职责一致。
- 后续添加 `NetAnimationSync`、`NetHealthSync` 时不需要修改位置同步代码。

## 后续阶段

第二级再考虑配置驱动装配：

```text
Inspector 模块列表 -> 运行时自动 AddComponent -> 自动注册 -> 自动同步
```

第一级先不做这一步，避免同时引入过多装配逻辑。
