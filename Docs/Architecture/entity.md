# EntitySystem

This document describes `Assets/Scripts/GamePlay/EntitySystem/`. It is a module reference; behavior rules are in [AGENTS.md](../../AGENTS.md).

> **Note:** This is an architecture snapshot. Names and subdirectories may drift; verify against the actual code.

## Responsibility

EntitySystem owns the lifecycle, input, modular capabilities, and state-machine-driven simulation of local/single-player entities. It bridges Unity object lifecycle to fixed-tick simulation but does **not** own network synchronization; that belongs to `GamePlay/NetworkSync/` as an explicit character replication runtime (see [simulation.md](simulation.md)).

## Directory Structure

```text
EntitySystem/
├── Controllers/             # Entity controllers
├── Input/                   # Input construction (`EntityInputCommandBuilder`)
├── Modules/                 # Entity capability MonoBehaviour components
│   ├── EntityModuleBase.cs
│   └── Position/            # MovementModule and position capabilities
├── Simulation/              # Local simulation drivers
├── StateMachine/            # Entity state machine
│   ├── BaseState/
│   └── EntityState/
└── Contracts/               # Entity contracts
```

## Core Abstractions

### Entity Host

- **`EntityCharacter`** is scene glue. It bridges Unity lifecycle to framework services and simulation systems; reusable business logic belongs in `SubSystemBase` or a pure core assembly.

### State Machine

- **`EntityBaseState`** in `StateMachine/BaseState/` owns `EntityContext` and exposes configuration, movement, input, speed, ground, sprint, run, and jump context.
- Concrete states in `StateMachine/EntityState/` (for example `EntityIdleState`) transition between `SPRINT`, `RUN`, and `WALK` based on input.
- `EntitySimulation` advances the state machine on every tick.

### Entity Modules

- **`EntityModuleBase`** is the base `MonoBehaviour` for capability modules attached to an entity GameObject.
- **`MovementModule`** in `Modules/Position/` owns entity position simulation and synchronization landing.

## Simulation Driver

EntitySystem uses fixed ticks rather than Unity `Update` directly:

- **`EntitySimulationSystem`** subscribes to `NetworkTimeSystem.Tick`, builds input, and calls `SimulateTick` for each local entity.
- **`EntitySimulation`** sets the current tick, records movement/aim input, updates rotation, and calls `StateMachine.Update(...)`.
- **`EntityInputCommandBuilder`** converts `InputState` into `EntityInputCommand`, including held keys and press edges.

The pure contracts and `Step` implementation are in `GamePlay/EntitySimulationCore/` (see [simulation.md](simulation.md)). EntitySystem depends on EntitySimulationCore; the reverse dependency is forbidden.

## Dependency Direction

```text
EntitySimulationCore (pure C#, no UnityEngine)
        ↑
EntitySystem (Unity lifecycle glue)
        ↑
NetworkSync (depends on EntitySystem + EntitySimulationCore)
```

EntitySystem depends on Framework and EntitySimulationCore, but not on NetworkSync.

## Assembly

EntitySystem currently compiles into the default `Assembly-CSharp`. `GamePlay.EntitySimulationCore` has an independent `.asmdef` with no engine reference. Document boundaries and direction before adding another `.asmdef`; see [AGENTS.md](../../AGENTS.md).
