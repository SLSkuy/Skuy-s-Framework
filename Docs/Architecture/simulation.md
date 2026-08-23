# Simulation Core and Network Synchronization

This document describes `Assets/Scripts/GamePlay/EntitySimulationCore/` and `Assets/Scripts/GamePlay/NetworkSync/`. It is a module reference; behavior rules are in [AGENTS.md](../../AGENTS.md).

> **Note:** This is an architecture snapshot. Names and subdirectories may drift; verify against the actual code.

## Responsibilities

- **`EntitySimulationCore/`** is C# and defines commands, state, simulation contracts, and fixed-tick utilities. It is the base for server authority, client prediction, and remote interpolation.
- **`NetworkSync/`** implements an explicit character synchronization runtime: authority simulation, owner prediction, remote interpolation, and character snapshots. It does not discover arbitrary network capabilities at runtime.

## EntitySimulationCore

### Assembly

`GamePlay.EntitySimulationCore.asmdef` is an independent assembly. It currently references Unity value types used by command and snapshot structs.

### Directory Structure

```text
EntitySimulationCore/
├── GamePlay.EntitySimulationCore.asmdef
├── Contracts/
├── Commands/
├── Prediction/
├── Snapshots/
└── state-related types
```

### Core Contracts

- **`IEntitySimulation`** exposes `Step(uint tick, float deltaTime, in EntityInputCommand command)` for one fixed-tick simulation.
- **`EntityInputCommand`** contains movement/aim input and held/pressed key state. `EntityInputCommandBuilder` builds it from `InputState` in EntitySystem.
- **`EntityCommandQueue`**, **`EntityPredictionHistory`**, and **`SnapshotBuffer`** are reusable tick primitives. They do not encode a generic entity protocol.
- **`EntitySimulation`** applies input, updates rotation/state, and resets tick flags.
- **`EntitySimulationSystem`** registers local simulation entries, listens to `NetworkTimeSystem.Tick`, builds commands, and calls `Simulation.Step`.

## NetworkSync

```text
NetworkSync/
├── Config/
├── Runtime/
│   ├── NetworkObjectIdentity.cs
│   ├── CharacterReplicationSystem.cs
│   ├── CharacterReplicationEntry.cs
│   ├── CharacterInputBuffer.cs
│   ├── CharacterPredictionController.cs
│   ├── CharacterSnapshotInterpolator.cs
│   ├── CharacterPresentationAdapter.cs
│   └── EntitySimulationMode.cs
└── TestSimulator/
```

### Simulation Modes

`EntitySimulationMode` defines the active role:

- `Authority`: server-authoritative simulation.
- `Predict`: client prediction for the input owner.
- `Replica`: remote snapshot interpolation.
- `LocalPlay`: local single-player simulation.

### Character replication

`NetworkObjectIdentity` holds `networkObjectId`, `ownerClientId`, and `EntitySimulationMode`. It notifies `CharacterReplicationSystem` on init/enable/disable. It does not scan sibling components.

`CharacterReplicationSystem` registers an explicit `CharacterReplicationEntry` per character. The entry binds:

- `NetworkObjectIdentity`
- `EntitySimulationObject`
- `PlayerController` or `AIController`
- `CharacterPresentationAdapter`
- input buffer, prediction history, and interpolation buffer

Call chain:

```text
NetworkObjectIdentity
  -> CharacterReplicationSystem.Register(character)

NetworkTimeSystem.Tick
  -> CharacterReplicationSystem.AdvanceTick(tick, deltaTime)
      -> authority input queue -> EntitySimulation.Step
      -> local input source -> EntitySimulation.Step + prediction history
```

The protocol uses `Player_Input`, `Character_Snapshot`, and `World_Snapshot` from `net_sync.proto`. `World_Snapshot` is a batch of character snapshots, not a generic channel container.

### Unity simulation assumption

Movement currently uses Unity `CharacterController`, `Transform`, and Unity value types. A future non-Unity dedicated server or strict cross-platform deterministic simulation will require a pure movement solver and collision abstraction. That work is outside the current character-sync prototype.

## Dependency Direction

```text
EntitySimulationCore (commands, history, snapshot buffer)
        ↑
EntitySystem (Unity character host and movement)
        ↑
NetworkSync (character replication runtime)
        ↑
Network (transport layer; see networking.md)
```

EntitySimulationCore has no gameplay-host dependency. NetworkSync depends on EntitySimulationCore and EntitySystem. It consumes Network messages after the transport layer receives them.

## Design Principle

Prefer “fail initialization loudly, expose the lifecycle error, then fix it” over self-healing. Keep tick/queue/snapshot primitives reusable; keep character synchronization explicit and game-specific. See [AGENTS.md](../../AGENTS.md).
