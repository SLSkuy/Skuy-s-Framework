## Context

动机见 `proposal.md`。行为契约见本 change 的 `specs/simulator/spec.md` 与 `specs/simulator/local-play/spec.md`。约束：`.cursor/rules/architecture.md` 与 `coding-style.md`；命名空间 `GamePlay.Simulator`；成员顺序参考 `NetworkObjectIdentity.cs`。

**已存在、本 change 当作前提：** `IInputStateProvider` 仅有 `GetInputState()`；`LocalInputProvider` 已直接实现该接口并从 Input System 组装 `InputState`。复制预测已只调 `GetInputState()`。

**仍待本 change 处理：** `Simulator` 持有 `_deviceInput` 与附身 id，Tick 内 `SubmitInput` 邮箱再平行 `_commandBuilders` 后 `Step`。`EntityInputBuffer` 窗口语义不改。

## Goals / Non-Goals

**Goals:**

- 槽位收敛为 class：身份、模拟角色、命令边沿、意图来源。
- Tick 收集段只调用槽位读取快照；Simulator 不持有设备模块、不按附身 id 分支。
- 复用已有 `IInputStateProvider`；设备与网络控制器挂同一口。
- LocalPlay 闭环可玩；网络控制器类型可挂槽位，缺采样返回空快照。

**Non-Goals:**

- 不改 `IInputStateProvider`、`LocalInputProvider`、`InputState` 的现有形态。
- 不另起 `IIntentSource` 类型树。
- 不改 `EntityInputBuffer` 窗口入队/消费及其内部 builder。
- 不把 `CharacterReplicationSystem.AdvanceTick` 换成槽位收集；不实现 ClientHost/ServerHost、预测回放、Replica 插值迁入核。
- 不在本 change 清理可能残留的未引用输入基类/旧接口文件。

## Decisions

### Decision 1: Tick 两段；快照来自槽位而非邮箱

`TickSystem.Tick` 仍只订阅一处。回调内：

1. **Collect**：已初始化可步进的槽位各取一份 `InputState`（`GetInputState` 或来源为空则 `default`），写入可复用字典。
2. **Simulate**：用冻结快照走槽位 `Build` 再 `Character.Step`。

不得在收集未完成时 `Step`。删除 `CommandMailbox` 与 `SubmitInput`。

备选：核内 `if possessed` 采样——否决，核会闭合设备模块。

### Decision 2: 直接使用已收口的 IInputStateProvider

本 change **不**再改 Framework 输入接口。槽位来源类型即为现有 `IInputStateProvider`。`InputState` 为纯快照结构。设备实现继续用 `LocalInputProvider.GetInputState()`。

备选：再包一层 `IIntentSource`——否决，与已收口接口重复。

### Decision 3: RegisteredEntity 持有来源与 builder

`RegisteredEntity` 为 class，构造创建 `EntityCommandBuilder`，来源字段为 `IInputStateProvider`（可 null）。

- `CollectIntent()`：来源 null → `default`，否则 `GetInputState()`。无 bool 失败。
- `Build(tick, input)` 转发 builder。
- `SetInputSource(provider)` 供 Host 绑定/解绑。

`Simulator` 删除 `_deviceInput`、`_possessedEntityId`、`SetDeviceInput`、`Possess` 采样。可保留按 id 转发 `SetInputSource` 的薄封装，但 MUST NOT 缓存提供器。`EntityRegistry` 存 class 引用。

备选：来源挂在 Character 上——否决，属于会话槽而非玩法组件。

### Decision 4: 网络控制器实现同一接口

新增 `GamePlay.Simulator` 下的网络输入控制器（建议名 `NetworkInputProvider`），实现已有 `IInputStateProvider`：

- `GetInputState()`：本拍无可用网络采样则返回 `default`。
- 入队/覆盖本拍快照的方法留在具体类上，**不**进入 `IInputStateProvider`。
- 本阶段 **不** 接入 `EntityInputBuffer.ConsumeNext`，**不** 替换复制系统权威消费。类型存在是为了槽位可挂、收集段无种类分支。

单机 Host：`Register` 后对该 pawn `SetInputSource(GameCore.LocalInput)`。Stop/解绑时来源置 null。

### Decision 5: EntityInputBuffer 与复制步进公式不动

缓冲窗口与内部 builder 保持。允许单机槽位边沿与联机缓冲边沿暂并存。后续 change 再让权威 `GetInputState` 走缓冲，并拆掉缓冲内 builder。

## Risks / Trade-offs

- [网络控制器与缓冲双路径] 本阶段未接线 → 接受；避免扩大联机回归。
- [来源 null 与「未初始化不步进」] 未 Init 的实体仍跳过收集/步进；已 Init 但无来源则空快照步进。二者不要混成收集失败。
- [换来源不 Reset 边沿] 附身切换可能少一拍边沿 → 本阶段 LocalPlay 一生一源；文档标明注销才丢边沿。

## Migration Plan

1. 改槽位与 Tick 两段，删邮箱与核内设备字段；落地 `NetworkInputProvider`。
2. LocalHost 绑定已有 `LocalInput`；Play 模式验证单机移动与 Start/Stop。
3. 回滚：还原 Simulator/槽位/邮箱即可；不改协议、不回滚已收口的 `IInputStateProvider`。

## Open Questions

- （无。缓冲接入网络控制器已明确推迟。）
