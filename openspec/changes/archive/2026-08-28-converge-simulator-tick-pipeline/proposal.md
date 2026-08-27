## Why

当前模拟核把「本拍意图」做成 `CommandMailbox` 的 Submit/Consume，但写入与消费发生在同一次 Tick 回调里，外部也无人调用 `SubmitInput`，邮箱没有独立生命周期。并行的 `_commandBuilders` 与核上的设备/附身字段让 Simulator 闭合了输入模块。`IInputStateProvider` 已收成只读 `InputState`，设备与网络控制器可以挂同一收集口；需要把槽位收成「身份 + 模拟 + 边沿 + 意图来源」，让核只向槽位取快照。

## What Changes

- 每个模拟 Tick MUST 先收集本拍所有可步进实体的 `InputState`，再构建命令并 `Step`。收集与模拟 MUST 分为两段。
- 模拟核 MUST NOT 判断收集成功或失败，MUST NOT 持有设备提供器或附身标识。槽位上的意图来源 MUST 经已有的 `IInputStateProvider.GetInputState` 交出快照；无来源或无采样时 MUST 使用默认空输入。
- **BREAKING**：删除 `CommandMailbox` 与 `Simulator.SubmitInput`。删除 Simulator 上的 `SetDeviceInput` / 附身采样特判。
- `RegisteredEntity` MUST 改为 class，持有命令构建与 `IInputStateProvider` 来源（可空，空则收集为空快照）。模拟核 MUST NOT 再维护平行 builder 字典。
- 本 change **不**再改 `IInputStateProvider`（已只含 `GetInputState`）。增加可挂到槽位上的网络输入控制器：实现同一接口；缺输入 MUST 返回空快照。本阶段 **不** 改 `EntityInputBuffer` 窗口语义，**不** 把复制系统 Tick 迁入本核。
- 单机 Host 在注册后把本机设备提供器赋给被附身槽位；未赋来源的可步进实体收集为空输入。

## Capabilities

### New Capabilities

- （无）

### Modified Capabilities

- `simulator`: Tick 先收集再模拟；注册槽持有命令边沿与意图来源；核只向槽位要快照；可挂网络控制器。
- `simulator/local-play`: 单机把设备提供器赋给被附身槽位，核按槽位收集后步进，不再经邮箱或核内附身特判。

## Impact

- **核**：`Simulator.cs`、`RegisteredEntity.cs`、`EntityRegistry.cs`；删除 `CommandMailbox.cs`；新增网络输入控制器类型。
- **输入**：不改 `IInputStateProvider` / `LocalInputProvider`。单机把现有 `GameCore.LocalInput` 挂到槽位。
- **单机 Host**：`LocalSimulationHost` 给槽位绑定 `LocalInput`，不再 `SetDeviceInput`。
- **联机**：`EntityInputBuffer` 窗口与内部 builder 本阶段不改；复制系统预测路径已只调 `GetInputState()`，本 change 不改其步进公式。网络控制器可先独立实现同一接口，暂不替换复制系统消费路径。
- **可玩性**：LocalPlay 移动/边沿与现网一致；无来源或网络控制器无采样时该实体本拍空命令仍步进。
