# EntitySystem

`Assets/Scripts/GamePlay/EntitySystem/` owns the entity domain and shared fixed-step simulation. It is independent from role-aware network synchronization.

## Responsibility

EntitySystem contains the scene character `EntityCharacter`, `EntityConfig`, input-to-command conversion, movement modules, locomotion states, rollback/simulation data, `PlayerController`, and the inactive `AIController` stub. It does not own network identity, snapshot buffering, snapshot transport, interpolation, or network time.

There is no generic `EntityObject` hierarchy. The only simulated scene type is `EntityCharacter`.

## Structure

```text
EntitySystem/
├── Config/             # EntityConfig
├── Entities/           # EntityCharacter, EntityContext, IEntityObjectIdentity
├── Commands/           # EntityInputCommand and EntityCommandQueue
├── Contracts/          # IEntitySimulation and IEntityStateStore
├── Prediction/         # EntityPredictionHistory and frames
├── State/              # EntitySimulationState, EntityRollbackState, MovementRollbackState
├── Controllers/        # PlayerController and AIController stub
├── Input/              # EntityInputCommandBuilder
├── Modules/            # MovementModule and RotationModule
├── Simulation/         # EntitySimulation
└── StateMachine/       # Locomotion state machine
```

## Core Abstractions

- `EntityCharacter` is the scene MonoBehaviour. It owns `EntityContext` and `EntitySimulation`, and exposes `Init`, `Step`, `CaptureSimulationState`, `CaptureRollbackState`, and `RestoreRollbackState`.
- `EntitySimulation` implements `IEntitySimulation` / `IEntityStateStore` and is the shared `Step` path used by LocalPlay, authority, prediction, and replay.
- `EntityInputCommandBuilder` converts sampled `InputState` into a fixed-tick `EntityInputCommand`.
- `EntityCommandQueue` and `EntityPredictionHistory` are data primitives; they do not encode a generic replication protocol. Replica snapshot buffering lives in MultiPlay `Interpolation/`.
- `PlayerController` samples local input. Role selection lives on `NetworkObjectIdentity`.
- `AIController` is an empty stub. No AI command policy is active.
- Dash fields on `MovementRollbackState` / `MovementModule` and `IsFocus` on `EntityContext` / `EntityRollbackState` are retained for later gameplay.

## Dependency Direction

```text
EntitySystem (entity domain and simulation)
        ↑
MultiPlaySystem (role-aware synchronization and network scene glue)
        ↑
Network (transport and message dispatch)
```

MultiPlaySystem consumes EntitySystem contracts and state. EntitySystem does not reference MultiPlaySystem.

## Lifecycle

`CharacterReplicationEntry.ApplyRole` calls `EntityCharacter.Init()`. The fixed tick is emitted by `MultiPlaySystem.NetworkTimeSystem`; EntitySystem does not subscribe to the network clock.
