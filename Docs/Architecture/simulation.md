# 模拟核心与网络同步

本文档描述 `Assets/Scripts/GamePlay/EntitySimulationCore/`（纯模拟核心）与 `Assets/Scripts/GamePlay/NetworkSync/`（网络同步）两个模块。属于模块设计文档 —— 行为规则见 [AGENTS.md](../../AGENTS.md)。

> **注意：** 以下内容为写作时的架构快照，类名与子目录会随重构漂移；以实际代码为准。

## 职责划分

- **`EntitySimulationCore/`**：纯 C# 模拟核心。定义命令、状态、模拟契约与核心 Tick 工具，**不依赖** UnityEngine。是「服务端权威 + 客户端预测 + 远端插值」架构的纯逻辑基底。
- **`NetworkSync/`**：网络同步层。基于 EntitySimulationCore 的模拟契约，实现权威模拟、客户端预测、远端插值与快照同步。依赖 EntitySimulationCore 与 EntitySystem。

## EntitySimulationCore

### 程序集

`GamePlay.EntitySimulationCore.asmdef`：独立程序集，**无引用**、`noEngineReferences: false` —— 即不依赖 Unity 引擎，是纯 C# 核心。

### 目录结构

```
EntitySimulationCore/
├── GamePlay.EntitySimulationCore.asmdef
├── Contracts/               # IEntitySimulation
├── Commands/                # EntityInputCommand
├── Simulation/              # EntitySimulation / EntitySimulationSystem
└── (state 相关)
```

### 核心契约

- **`IEntitySimulation`**（`Contracts/`）：模拟入口。`Step(uint tick, float deltaTime, in EntityInputCommand command)` 驱动单实体固定 Tick 模拟。
- **`EntityInputCommand`**（`Commands/`）：每 Tick 输入命令，含 move/aim 输入与 held/pressed 按键状态。由 EntitySystem 的 `EntityInputCommandBuilder` 从 `InputState` 构建。
- **`EntitySimulation`**（`Simulation/`）：`IEntitySimulation.Step` 的具体实现 —— 应用输入、更新旋转/状态机、重置 Tick 标志。
- **`EntitySimulationSystem`**（`Simulation/`）：注册本地模拟入口，挂钩 `NetworkTimeSystem.Tick`，构建命令并调用 `Simulation.Step`。

## NetworkSync

### 目录结构

```
NetworkSync/
├── Config/                  # 同步配置
├── Runtime/                 # 运行时能力与编排
│   ├── EntityReplicationSystem.cs
│   ├── EntitySimulationMode.cs
│   └── Capabilities/        # NetworkTransformCapability / NetworkPredictionCapability / NetworkInterpolationCapability
└── Snapshot/                # SnapshotInterpolator 等快照插值
```

### 模拟角色

**`EntitySimulationMode`**（`Runtime/`）定义同步中的模拟角色：

- `Authority` —— 服务端权威模拟。
- `Predict` —— 客户端预测（本地拥有输入）。
- `Replica` —— 远端插值（仅接收快照）。
- `LocalPlay` —— 单机本地。

### 编排

**`EntityReplicationSystem`**（`Runtime/`）：网络同步编排器。根据激活的 capability 推进权威模拟或拥有方预测：

- 权威路径与预测路径都调用 `entry.Simulation.Step(...)`（依赖 EntitySimulationCore）。
- 预测路径额外记录回滚状态。

### 能力（Capabilities）

能力是按角色启用的同步组件，声明对 `InputCommand` / `Simulation` / `Snapshot` / `Transform` 的依赖：

- **`NetworkPredictionCapability`**：仅支持 `EntitySimulationMode.Predict`，依赖 `InputCommand` / `Simulation` / `Snapshot`。
- **`NetworkInterpolationCapability`**：仅支持 `EntitySimulationMode.Replica`，依赖 `Transform` / `Snapshot`。
- **`NetworkTransformCapability`**：Transform 同步能力。

### 快照插值

**`SnapshotInterpolator`**（`Snapshot/`）：为远端 replica 实现快照插值。添加服务端快照、采样插值后的 `EntitySimulationState`。

## 依赖方向

```
EntitySimulationCore（纯 C#，无 UnityEngine）
        ↑
NetworkSync（依赖 EntitySimulationCore + EntitySystem）
        ↑
Network（传输层，见 networking.md）
```

- EntitySimulationCore 不依赖任何上层。
- NetworkSync 依赖 EntitySimulationCore（模拟契约）与 EntitySystem（实体宿主）。
- NetworkSync 不直接依赖 Network 传输层；网络消息经 Network 收发后，由 NetworkSync 侧消费。

## 架构理念

整体遵循「初始化失败 → 暴露错误 → 修复生命周期」而非 self-healing —— 见 [AGENTS.md](../../AGENTS.md)「生命周期」。模拟核心保持纯 C# 以便服务端/客户端/测试复用，Unity 胶水留在 EntitySystem。
