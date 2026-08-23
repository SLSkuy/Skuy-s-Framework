# Simulation Core and Network Synchronization

This document describes `Assets/Scripts/GamePlay/EntitySimulationCore/` and `Assets/Scripts/GamePlay/NetworkSync/`. It is a module reference; behavior rules are in [AGENTS.md](../../AGENTS.md).

> **Note:** This is an architecture snapshot. Names and subdirectories may drift; verify against the actual code.

## Responsibilities

- **`EntitySimulationCore/`** is pure C# and defines commands, state, simulation contracts, and fixed-tick utilities. It has no `UnityEngine` dependency and is the base for server authority, client prediction, and remote interpolation.
- **`NetworkSync/`** implements authority simulation, client prediction, remote interpolation, and snapshot synchronization on top of EntitySimulationCore and EntitySystem.

## EntitySimulationCore

### Assembly

`GamePlay.EntitySimulationCore.asmdef` is an independent assembly with no engine reference (`noEngineReferences: false` in the current snapshot); it is pure C#.

### Directory Structure

```text
EntitySimulationCore/
├── GamePlay.EntitySimulationCore.asmdef
├── Contracts/
├── Commands/
├── Simulation/
└── state-related types
```

### Core Contracts

- **`IEntitySimulation`** exposes `Step(uint tick, float deltaTime, in EntityInputCommand command)` for one fixed-tick simulation.
- **`EntityInputCommand`** contains movement/aim input and held/pressed key state. `EntityInputCommandBuilder` builds it from `InputState` in EntitySystem.
- **`EntitySimulation`** applies input, updates rotation/state, and resets tick flags.
- **`EntitySimulationSystem`** registers local simulation entries, listens to `NetworkTimeSystem.Tick`, builds commands, and calls `Simulation.Step`.

## NetworkSync

```text
NetworkSync/
├── Config/
├── Runtime/
│   ├── EntityReplicationSystem.cs
│   ├── EntitySimulationMode.cs
│   └── Capabilities/
└── Snapshot/
```

### Simulation Modes

`EntitySimulationMode` defines the active role:

- `Authority`: server-authoritative simulation.
- `Predict`: client prediction for the input owner.
- `Replica`: remote snapshot interpolation.
- `LocalPlay`: local single-player simulation.

### Orchestration and Capabilities

`EntityReplicationSystem` advances authority or owner prediction through `entry.Simulation.Step(...)`; prediction additionally records rollback state.

Capabilities are role-specific synchronization components:

- `NetworkPredictionCapability`: `Predict`, requiring input command, simulation, and snapshot support.
- `NetworkInterpolationCapability`: `Replica`, requiring transform and snapshot support.
- `NetworkTransformCapability`: transform synchronization.

`SnapshotInterpolator` stores server snapshots and samples interpolated `EntitySimulationState` for remote replicas.

## Dependency Direction

```text
EntitySimulationCore (pure C#, no UnityEngine)
        ↑
NetworkSync (EntitySimulationCore + EntitySystem)
        ↑
Network (transport layer; see networking.md)
```

EntitySimulationCore has no upper-layer dependency. NetworkSync depends on EntitySimulationCore and EntitySystem, but does not directly depend on Network transport; it consumes messages after Network receives them.

## Design Principle

Prefer “fail initialization loudly, expose the lifecycle error, then fix it” over self-healing. Keep the simulation core pure C# so server, client, and tests can reuse it; keep Unity glue in EntitySystem. See [AGENTS.md](../../AGENTS.md).
