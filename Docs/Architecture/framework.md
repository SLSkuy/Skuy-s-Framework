# Framework 核心框架

本文档描述 `Assets/Scripts/Framework/` 模块的架构。属于模块设计文档 —— 行为规则见 [AGENTS.md](../../AGENTS.md)，通用架构概览见 [.agents/rules/Architecture.md](../../.agents/rules/Architecture.md)。

> **注意：** 以下内容为写作时的架构快照，类名与子目录会随重构漂移；以实际代码为准。

## 职责

Framework 是可复用的核心框架层，提供：服务定位器、子系统基类与生命周期、单例、状态机、事件总线、数据代理、以及一批可复用子系统（Camera/UI/Resource/SceneControl/Timer/ObjectPool/States/DataProxy）。业务代码（GamePlay/Network）通过 Framework 暴露的静态入口获取能力，不直接持有框架内部状态。

## 目录结构

```
Framework/
├── Global.cs                 # 全局服务定位器
├── Common/                   # 公共基础
│   ├── Singleton/            # MonoSingleton<T>
│   ├── SubSystemManager/     # SubSystemBase / SystemManager / ISubSystem
│   └── StateMachine/         # IState / EnumStateBase / ExtendableStateBase
├── SubSystems/               # 可复用子系统集合
│   ├── Camera/  DataProxy/  ObjectPool/  Resource/
│   ├── SceneControl/  States/  Timer/  UI/
├── Input/                    # 输入抽象
├── Navigation/               # 基于 ECS 的 A* 寻路
└── Event/                    # EventBus / EventHub
```

## 核心抽象

### 服务定位器 + 子系统

- **`Global`**（`Global.cs`）：静态服务定位器。业务入口：
  - `Global.Get<T>()` / `Global.TryGet<T>(out var s)` —— 获取 `SubSystemBase` 子系统。
  - `Global.GetDataProxy<T>()` / `Global.TryGetDataProxy<T>()` —— 获取 `IDataProxy`。
- **`SubSystemBase`**（`Common/SubSystemManager/SubSystemBase.cs`）：所有子系统的抽象基类。构造时自注册到 `Global`。
  - 生命周期由**不可重写**的 `_Init()` / `_Destroy()` 驱动（在 `SystemManager` 注册/销毁时调用）。
  - 子类重写虚方法：`Init()` / `Destroy()` / `BindEvents()` / `Update()` / `FixedUpdate()` / `LateUpdate()`。
  - 每个子系统暴露唯一的 `SubSystemPriority`，`SystemManager` 据此排序初始化。
- **`SystemManager`**：注册、排序、驱动所有 `ISubSystem` 的生命周期。

### MonoBehaviour 单例

- **`MonoSingleton<T>`**（`Common/Singleton/MonoSingleton.cs`）：通过 `MonoSingleton<T>.Instance` 访问。子类重写 `Init()` / `Destroy()`，应用退出时调用 `ShutDown()`。

### 状态机

- **`IState`** / **`EnumStateBase<TEnum>`** / **`ExtendableStateBase`**（`Common/StateMachine/`）。
- `Update()` 内部调用 `Tick()` 再调用 `CheckStateChange()`；子类重写 `Tick` / `CheckStateChange` 而非 `Update`。
- 具体状态用 `...State` 后缀（如 `EntityIdleState`、`MainMenuState`）。

### 事件总线

- **`EventBus`**（`Event/EventBus.cs`）：`EventBus.Get<TEvent>()` 按事件类型获取/绑定事件对象，再 `.Dispatch(data)` 分发。
- 类内直接订阅用 `event Action<T>`。

### 数据代理

- **`IDataProxy`**：通过 `DataProxyManager` 注册，经 `Global.GetDataProxy<T>()` 访问。

## 子系统清单

`SubSystems/` 下实际存在的子系统（写作时）：

- `Camera/`、`DataProxy/`、`ObjectPool/`、`Resource/`、`SceneControl/`、`States/`、`Timer/`、`UI/`

业务代码不直接 `new` 这些子系统，统一经 `Global.Get<T>()` 获取。

## 依赖方向

Framework 是最底层可复用层，**不依赖** GamePlay / Network。GamePlay 与 Network 单向依赖 Framework。

## 程序集

Framework 模块当前编译进默认 `Assembly-CSharp`（无独立 `.asmdef`）。若未来按职责拆分程序集，须保持 Framework 不依赖上层模块 —— 见 [AGENTS.md](../../AGENTS.md)「依赖方向」。
