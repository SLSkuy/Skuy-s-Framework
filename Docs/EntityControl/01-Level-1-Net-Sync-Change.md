# 第一级网络同步变更文档

## 目标

当前第一级目标是建立一个最小但稳定的网络同步入口：实体只需要挂载一个统一的网络同步根组件，它会自动扫描当前实体及子物体上的可同步组件，并按实体网络角色完成注册和同步驱动。

本阶段不追求完整 authoring 配置驱动，也不追求自动添加所有同步组件。第一级先实现“已有同步组件自动发现、自动注册、自动分发角色、自动进入同步流程”。

## 期望使用方式

实体根节点只需要挂载一个统一入口组件，例如：

```text
NetEntitySyncRoot
```

同步能力以普通组件形式挂在实体根节点或子节点上，例如：

```text
NetPositionSync
NetAnimationSync
NetSkillSync
NetHealthSync
```

运行时由 `NetEntitySyncRoot` 自动扫描所有实现同步接口的组件，并完成注册。

## 第一级能力范围

### 包含

- 自动扫描实体及子节点上的同步组件。
- 自动注册同步组件。
- 根据 `NetEntityRole` 分发角色。
- 支持位置同步组件。
- 支持后续动画、技能、生命值等同步组件扩展。
- 支持同步模块按角色启停。

### 不包含

- 不在本阶段实现 Inspector 勾选后自动 `AddComponent`。
- 不在本阶段实现完整技能同步。
- 不在本阶段实现旋转同步。
- 不在本阶段重写网络协议。
- 不在本阶段处理 yaw / pitch 的同步策略。

## 核心结构

推荐将当前分散的注册和角色分发职责合并为一个统一入口：

```text
NetEntitySyncRoot
  - 持有 NetEntityIdentity
  - 扫描 INetSyncComponent
  - 建立 SyncModuleID 到模块实例的映射
  - 在角色变化时调用 ConfigureRole
  - 在 Tick / Update 中驱动需要运行的同步模块
```

如果暂时保留现有拆法，也应形成等价结构：

```text
NetEntityIdentity
NetSyncModuleRegistry
NetEntityRoleAssembler
```

但长期建议收束成一个更明确的根组件，减少 prefab 配置数量。

## 同步组件接口建议

第一级同步组件至少需要支持：

```csharp
public interface INetSyncComponent
{
    SyncModuleID ModuleId { get; }
    void ConfigureRole(NetEntityRole role);
}
```

需要捕获快照的组件实现：

```csharp
public interface INetSyncSnapshotSource<TSnapshot>
{
    TSnapshot CaptureSnapshot(uint snapshotTick, uint lastProcessedInputTick = 0);
}
```

需要接收快照的组件实现：

```csharp
public interface INetSyncSnapshotReceiver<TSnapshot>
{
    void ApplySnapshot(in TSnapshot snapshot);
}
```

需要插值的组件实现：

```csharp
public interface INetInterpolatedSync
{
    void UpdateInterpolation(float deltaTime);
}
```

## 位置同步变更

当前 `NetTransformSync` 需要收窄职责并改名为：

```text
NetPositionSync
```

原因：

- 当前同步内容只应该关注位置。
- 旋转与角色 yaw / pitch、朝向控制、相机输入、模型朝向有关。
- 在 yaw / pitch 策略未稳定前同步 rotation 容易污染预测、插值和动画表现。

## 旋转同步处理

本阶段从位置同步中移除 rotation。

需要移除或停止使用：

- `NetTransformSnapshot.Rotation`
- `CaptureSnapshot` 中的 `Rotation = transform.eulerAngles`
- `ApplySnapshot` 中的 `Quaternion.Euler(snapshot.Rotation)`
- `ApplyInterpolatedSnapshot` 中的旋转插值
- `transform.SetPositionAndRotation(...)`

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

## 命名变更

需要改名：

| 当前名称 | 新名称 | 说明 |
| --- | --- | --- |
| `NetTransformSync` | `NetPositionSync` | 只负责位置同步 |
| `NetTransformSnapshot` | `NetPositionSnapshot` | 只承载位置快照 |
| `SyncModuleID.Transform` | `SyncModuleID.Position` | 模块语义收窄 |

如果协议层暂时仍使用 `Transform_Snapshot`，可以先保留协议名不变，在转换层屏蔽 rotation 字段。不要手动修改 generated protobuf 文件。

## 自动扫描注册流程

第一级推荐流程：

1. `NetEntitySyncRoot.Awake`
2. 获取 `NetEntityIdentity`
3. 扫描 `GetComponentsInChildren<MonoBehaviour>(true)`
4. 过滤所有 `INetSyncComponent`
5. 写入模块列表和 `SyncModuleID` 字典
6. 订阅 `NetEntityIdentity.RoleChanged`
7. 对所有模块调用 `ConfigureRole(identity.Role)`
8. 后续角色变化时重新调用 `ConfigureRole`

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

第一级完成时应满足：

- 实体只需要挂载一个统一网络同步入口组件。
- 已挂载的同步能力组件可以被自动扫描。
- 同步组件可以按 `SyncModuleID` 查询。
- 角色变化会自动分发到所有同步组件。
- 位置同步不再处理 rotation。
- `NetPositionSync` 名称与职责一致。
- 后续添加 `NetAnimationSync`、`NetSkillSync` 时不需要修改位置同步代码。

## 后续阶段

第二级再考虑配置驱动装配：

```text
Inspector 模块列表 -> 运行时自动 AddComponent -> 自动注册 -> 自动同步
```

第一级先不做这一步，避免同时引入过多装配逻辑。

