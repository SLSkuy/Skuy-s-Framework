# EntitySystem

`Assets/Scripts/GamePlay/EntitySystem/` owns the entity domain and shared fixed-step simulation. It is independent from role-aware network synchronization.

## Responsibility

EntitySystem contains entity objects, configuration, input-to-command conversion, movement modules, state machine states, rollback/simulation data, player input, and the inactive AI controller extension point. It does not own network identity, snapshot transport, prediction policy, or network time.

## Structure

```text
EntitySystem/
├── Config/             # EntityConfig and EntityObjectConfig
├── Entities/           # EntityCharacter and EntityObject hierarchy
├── Commands/           # EntityInputCommand and EntityCommandQueue
├── Contracts/          # IEntitySimulation and state contracts
├── Prediction/         # EntityPredictionHistory and frames
├── Snapshots/          # SnapshotBuffer and entity snapshot contract
├── State/              # Simulation and rollback state data
├── Controllers/        # PlayerController and future AIController hook
├── Input/              # EntityInputCommandBuilder
├── Modules/            # Movement and rotation modules
├── Simulation/         # EntitySimulation
└── StateMachine/       # Locomotion state machine
```

## Core Abstractions

- `EntitySimulation` implements the shared `IEntitySimulation.Step` path used by LocalPlay, server authority, client prediction, and prediction replay.
- `EntityInputCommandBuilder` converts sampled `InputState` into a fixed-tick `EntityInputCommand`.
- `EntityCommandQueue`, `EntityPredictionHistory`, and `SnapshotBuffer` are data primitives; they do not encode a generic replication protocol.
- `PlayerController` samples local input and exposes it to the role-aware scheduler. It does not register or own a simulation system.
- `AIController` remains a disabled future extension point. No AI behavior or command policy is active in the prototype.

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

Unity scene objects bind to EntitySystem components during replication entry setup. The fixed tick is emitted by `MultiPlaySystem.NetworkTimeSystem`; `EntitySystem` supplies the simulation implementation but does not subscribe to the network clock directly.
