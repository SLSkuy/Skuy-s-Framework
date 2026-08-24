# EntitySystem

`Assets/Scripts/GamePlay/EntitySystem/` owns the entity domain and shared fixed-step simulation. It is independent from role-aware network synchronization.

## Responsibility

EntitySystem contains the scene character `EntityCharacter`, `EntityConfig`, input-to-command conversion, transform/view modules, locomotion states, rollback/simulation data, `PlayerController`, and the inactive `AIController` stub. It does not own network identity, snapshot buffering, snapshot transport, interpolation, or network time.

There is no generic `EntityObject` hierarchy. The only simulated scene type is `EntityCharacter`.

## Structure

```text
EntitySystem/
├── Config/             # EntityConfig
├── Entities/           # EntityCharacter, EntityContext, IEntityObjectIdentity
├── Commands/           # EntityInputCommand and EntityCommandQueue
├── Contracts/          # IEntitySimulation and IEntityStateStore
├── Prediction/         # EntityPredictionHistory and frames
├── Snapshot/State/     # EntitySimulationState, EntityRollbackState, MovementRollbackState, ViewRollbackState
├── Controllers/        # PlayerController and AIController stub
├── Input/              # EntityInputCommandBuilder
├── Modules/            # TransformModule and ViewModule
├── Simulation/         # EntitySimulation
└── LocomotionStateMachine/ # Locomotion state machine
```

## Core Abstractions

- `EntityCharacter` is the scene MonoBehaviour. It owns `EntityContext` and `EntitySimulation`, and exposes `Init`, `Step`, `CaptureSimulationState`, `CaptureRollbackState`, and `RestoreRollbackState`.
- `EntitySimulation` implements `IEntitySimulation` / `IEntityStateStore` and is the shared `Step` path used by LocalPlay, authority, prediction, and replay.
- `EntityInputCommandBuilder` converts sampled `InputState` into a fixed-tick `EntityInputCommand`.
- `EntityCommandQueue` is a client-tick-indexed ring buffer primitive (slot 0 means empty). Sequential consume, `LastProcessedTick`, and past/future receive windows live on MultiPlay `CharacterInputBuffer`. Missing slots yield a default command. `EntityPredictionHistory` is a data primitive. Neither encodes a generic replication protocol. Replica snapshot buffering lives in MultiPlay `Interpolation/`.
- `PlayerController` samples local input. Role selection lives on `NetworkObjectIdentity`.
- `AIController` is an empty stub. No AI command policy is active.
- Dash fields on `MovementRollbackState` / `TransformModule` and `IsFocus` on `EntityContext` / `EntityRollbackState` are retained for later gameplay.
- `ViewModule` lives on the entity root and binds the `orientation` child with `Transform.Find` during `Init`. Aim input is a per-tick look delta. `TransformModule` keeps root rotation identity, turns child `mesh` toward the orientation-mapped move target, and translates along current mesh facing.

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

`CharacterReplicationEntry.ApplyRole` calls `EntityCharacter.Init()`. Init order is `InitConfig` -> `InitCapacityModule` -> `InitSimulationContext` -> `RegisterStates`. The fixed tick is emitted by `MultiPlaySystem.NetworkTimeSystem`; EntitySystem does not subscribe to the network clock.
