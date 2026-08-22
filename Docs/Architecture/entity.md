# EntitySystem 实体系统

本文档描述 `Assets/Scripts/GamePlay/EntitySystem/` 模块的架构。属于模块设计文档 —— 行为规则见 [AGENTS.md](../../AGENTS.md)。

> **注意：** 以下内容为写作时的架构快照，类名与子目录会随重构漂移；以实际代码为准。

## 职责

EntitySystem 负责**单机/本地实体的生命周期、输入、模块化能力与状态机驱动**。它把 Unity 对象生命周期与固定 Tick 模拟衔接起来，但**不**包含网络同步逻辑（网络同步在 `GamePlay/NetworkSync/`，见 [simulation.md](simulation.md)）。

## 目录结构

```
EntitySystem/
├── Controllers/             # 实体控制器
├── Input/                   # 输入构建（EntityInputCommandBuilder）
├── Modules/                 # 实体能力模块（MonoBehaviour 组件化）
│   ├── EntityModuleBase.cs  # 模块基类（继承 MonoBehaviour）
│   └── Position/            # MovementModule 等位置相关模块
├── Simulation/              # 本地模拟驱动（EntitySimulationSystem / EntitySimulation）
├── StateMachine/            # 实体状态机
│   ├── BaseState/           # EntityBaseState
│   └── EntityState/         # EntityIdleState 等具体状态
└── Contracts/               # 实体相关契约
```

## 核心抽象

### 实体宿主

- **`EntityCharacter`**：MonoBehaviour，场景胶水。只负责把 Unity 生命周期桥接到框架服务与模拟系统，**不**包含可复用业务逻辑。可复用逻辑应落在 `SubSystemBase` 或纯核心程序集（见 [AGENTS.md](../../AGENTS.md)「场景胶水 vs 核心」）。

### 状态机

- **`EntityBaseState`**（`StateMachine/BaseState/`）：实体状态基类，持有 `EntityContext`，暴露配置、移动、输入、速度、接地、冲刺、奔跑、跳跃等上下文属性。
- **具体状态**（`StateMachine/EntityState/`）：如 `EntityIdleState`，根据移动输入 / Sprint / Run 在 `SPRINT`、`RUN`、`WALK` 之间转移。
- 状态机由 `EntitySimulation` 在每个 Tick 调用 `StateMachine.Update(...)` 推进。

### 实体模块

- **`EntityModuleBase`**：继承 `MonoBehaviour`，是实体能力模块的基类。模块是 Unity 组件化架构的一部分，挂在实体 GameObject 上。
- **`MovementModule`**（`Modules/Position/`）：负责实体位置模拟与同步落点。

## 模拟驱动

EntitySystem 的本地模拟由固定 Tick 驱动，**不是**直接由 Unity `Update` 推进：

- **`EntitySimulationSystem`**（`Simulation/`）：全局固定 Tick 驱动系统。初始化时订阅 `NetworkTimeSystem.Tick`，每个 Tick 调用 `SimulateTick`，为每个本地实体生成输入并调用 `Simulation.Step(...)`。
- **`EntitySimulation`**（`Simulation/`）：单个实体的 Tick 推进逻辑。设置当前 Tick、记录移动/瞄准输入、更新旋转、调用 `StateMachine.Update(...)`。
- 输入由 **`EntityInputCommandBuilder`**（`Input/`）从 `InputState` 构建 `EntityInputCommand`，计算 held 按键与 press 边沿后喂给模拟核心。

> 模拟核心的纯契约与 `Step` 实现位于 `GamePlay/EntitySimulationCore/`（见 [simulation.md](simulation.md)）。EntitySystem 依赖 EntitySimulationCore，反向不依赖。

## 依赖方向

```
EntitySimulationCore（纯核心，无 UnityEngine）
        ↑
EntitySystem（含 Unity 生命周期胶水）
        ↑
NetworkSync（网络同步，依赖 EntitySystem + EntitySimulationCore）
```

EntitySystem 依赖 Framework（经 `Global` 取子系统）与 EntitySimulationCore（取模拟契约）。EntitySystem **不依赖** NetworkSync。

## 程序集

EntitySystem 当前编译进默认 `Assembly-CSharp`。`GamePlay.EntitySimulationCore` 有独立 `.asmdef`（无引擎引用，纯 C#）。新增 `.asmdef` 须文档化边界与方向 —— 见 [AGENTS.md](../../AGENTS.md)「依赖方向」。
