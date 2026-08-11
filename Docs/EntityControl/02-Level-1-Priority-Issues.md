# 第一级网络同步优先问题

## 背景

本文件只记录当前第一级网络同步实现中需要优先处理的三个问题。

其他架构问题、实体基类问题、技能系统接入问题后续再单独处理。

## 问题一：`NetEntitySyncRoot` 还没有成为唯一同步入口

### 当前现象

`NetEntitySyncRoot` 已经负责：

- 扫描同步组件
- 注册同步组件
- 查询同步组件
- 分发 `NetEntityRole`
- 驱动 `INetSyncUpdatable.SyncUpdate`

但当前控制器仍然直接依赖 `NetPositionSync`：

- `PlayerController`
- `AuthorityController`
- `ReplicaController`

这些控制器仍通过字段直接调用：

- `_positionSync.CaptureSnapshot(...)`
- `_positionSync.ApplySnapshot(...)`
- `_positionSync.UpdateInterpolation(...)`

因此，位置同步仍然被控制器硬编码驱动，`NetEntitySyncRoot` 还不是同步系统的唯一入口。

### 影响

- 不能完全满足“只挂一个网络同步根组件即可自动注册并执行同步”的目标。
- 新增同步模块时，控制器可能仍需要新增字段或直接依赖。
- 同步能力仍然和特定控制器耦合。

### 修复目标

控制器不直接依赖具体同步组件，而是通过 `NetEntitySyncRoot` 查询同步模块。

例如：

```text
PlayerController
  -> NetEntitySyncRoot
    -> NetPositionSync
```

而不是：

```text
PlayerController
  -> NetPositionSync
```

### 建议处理方式

第一步：

- 控制器依赖 `NetEntitySyncRoot`。
- 通过 `TryGetModule(ModuleType.Position, out NetPositionSync module)` 获取位置同步模块。

第二步：

- 预测、权威、复制体逻辑继续留在控制器中。
- 但所有同步模块访问统一经过 `NetEntitySyncRoot`。

第三步：

- 后续再把预测、回放、插值调度从控制器继续移出。

## 问题二：`NetPositionSync.OnAuthoritySnapshot` 存在空引用风险

### 当前现象

`NetPositionSync.OnAuthoritySnapshot` 中直接调用：

```csharp
_snapshots.Add(snapshot);
```

但 `_snapshots` 只在 `ConfigureRole(NetEntityRole.Replica)` 中初始化。

如果快照早于角色配置到达，或者外部在非 `Replica` 状态调用 `OnAuthoritySnapshot`，会出现空引用风险。

### 影响

- 网络快照到达时序稍有变化就可能触发异常。
- 第一级同步入口尚未完全统一调度时，这个风险更明显。
- 运行时角色切换也可能导致缓冲区被清空后又继续接收快照。

### 修复目标

`OnAuthoritySnapshot` 必须对当前状态做防御性检查。

### 建议处理方式

可选方案一：非 `Replica` 直接拒绝。

```text
如果当前角色不是 Replica，则忽略权威快照。
```

可选方案二：快照缓冲区为空时延迟初始化。

```text
如果 _snapshots 为空，则初始化 Replica 缓冲区。
```

推荐方案：

- `ConfigureRole` 仍然负责主要初始化。
- `OnAuthoritySnapshot` 增加兜底保护。
- 非 `Replica` 状态下不消费权威快照。

## 问题三：位置同步语义已收窄，但协议转换命名仍保留 Transform

### 当前现象

运行时代码已经从：

```text
NetTransformSync
NetTransformSnapshot
SyncModuleID.Transform
```

收窄为：

```text
NetPositionSync
NetPositionSnapshot
ModuleType.Position
```

但协议转换工具中仍保留：

```csharp
ToTransformSnapshot(...)
```

底层 protobuf 仍然使用：

```text
Transform_Snapshot
```

### 影响

- 命名语义容易误导后续开发。
- 其他人可能误以为 rotation 仍在同步流程中。
- 位置同步和协议名称之间缺少明确边界。

### 修复目标

运行时代码全部使用 Position 语义。

协议层可以暂时保留 `Transform_Snapshot`，但转换工具方法需要表达“这是位置同步协议映射”。

### 建议处理方式

不要手动修改 generated protobuf 文件。

建议将工具方法改名为：

```text
ToPositionSnapshotMessage(...)
ToNetPositionSnapshot(...)
```

并在方法注释中说明：

```text
当前协议仍复用 Transform_Snapshot，但运行时只读写 Position / Velocity / MovementState，不处理 Rotation。
```

## 优先级

建议按以下顺序处理：

1. 修复 `OnAuthoritySnapshot` 空引用风险。
2. 让控制器通过 `NetEntitySyncRoot` 访问同步模块。
3. 清理协议转换工具命名。

## 完成标准

完成后应满足：

- 控制器不再直接硬依赖 `NetPositionSync`。
- `NetEntitySyncRoot` 是同步组件访问与注册的统一入口。
- `NetPositionSync` 在任意快照到达时序下不会因 `_snapshots` 为空崩溃。
- 运行时代码和工具方法都使用 Position 语义。
- 生成协议文件不被手动修改。

