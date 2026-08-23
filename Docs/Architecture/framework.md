# Framework Core

This document describes `Assets/Scripts/Framework/`. It is a module reference; behavior rules are in [AGENTS.md](../../AGENTS.md), and the general architecture overview is in [`.agents/rules/Architecture.md`](../../.agents/rules/Architecture.md).

> **Note:** This is an architecture snapshot. Names and subdirectories may drift; verify against the actual code.

## Responsibility

Framework is the reusable foundation layer. It provides the service locator, subsystem base and lifecycle, singletons, state machines, event bus, data proxies, and reusable subsystems such as Camera, UI, Resource, SceneControl, Timer, ObjectPool, States, and DataProxy. GamePlay and Network access these capabilities through Framework's public/static entry points.

## Directory Structure

```text
Framework/
├── Global.cs
├── Common/
│   ├── Singleton/
│   ├── SubSystemManager/
│   └── StateMachine/
├── SubSystems/
│   ├── Camera/  DataProxy/  ObjectPool/  Resource/
│   ├── SceneControl/  States/  Timer/  UI/
├── Input/
├── Navigation/               # ECS-based A* navigation
└── Event/                    # EventBus / EventHub
```

## Core Abstractions

### Service Locator and Subsystems

- **`Global`** provides `Global.Get<T>()` / `Global.TryGet<T>(out var s)` for `SubSystemBase`, and `Global.GetDataProxy<T>()` / `Global.TryGetDataProxy<T>()` for `IDataProxy`.
- **`SubSystemBase`** is the abstract base for all subsystems. It registers with `Global`; non-overridable `_Init()` / `_Destroy()` drive lifecycle. Subclasses override `Init()`, `Destroy()`, `BindEvents()`, `Update()`, `FixedUpdate()`, and `LateUpdate()`.
- **`SystemManager`** registers, sorts, and drives every `ISubSystem` by `SubSystemPriority`.

### MonoBehaviour Singleton

`MonoSingleton<T>` is accessed through `MonoSingleton<T>.Instance`. Subclasses override `Init()` / `Destroy()` and call `ShutDown()` during application exit.

### State Machine

`IState`, `EnumStateBase<TEnum>`, and `ExtendableStateBase` live under `Common/StateMachine/`. `Update()` calls `Tick()` and then `CheckStateChange()`; concrete states use the `...State` suffix.

### Event Bus and Data Proxy

`EventBus.Get<TEvent>().Dispatch(data)` dispatches typed events; class-local subscriptions use `event Action<T>`. `IDataProxy` instances are registered by `DataProxyManager` and accessed through `Global.GetDataProxy<T>()`.

## Subsystems

The current `SubSystems/` directories are `Camera/`, `DataProxy/`, `ObjectPool/`, `Resource/`, `SceneControl/`, `States/`, `Timer/`, and `UI/`. Business code should retrieve them through `Global.Get<T>()`, not construct them directly.

## Dependency Direction

```text
Framework (Global / SubSystemBase / reusable services)
        ↑
GamePlay and Network
```

Framework must not depend on GamePlay or Network.

## Assembly

Framework currently compiles into the default `Assembly-CSharp` without an independent `.asmdef`. Any future split must preserve the lower-layer dependency direction; see [AGENTS.md](../../AGENTS.md).
