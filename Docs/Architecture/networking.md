# Network Layer and MultiPlaySystem

## Network Responsibility

`Assets/Scripts/Network/` owns transport-independent client/server entry points, KCP/TCP transport abstractions, protobuf serialization, and message dispatch. It does not own gameplay entity simulation or replication policy.

## MultiPlaySystem Responsibility

`Assets/Scripts/GamePlay/MultiPlaySystem/` is the game-specific synchronization runtime. Logical layout:

```text
MultiPlaySystem/
├── Config/           # SyncConfig
├── Time/             # NetworkTimeSystem, NetworkTickSystem
├── Identity/         # NetworkObjectIdentity, EntitySimulationMode
├── Replication/      # CharacterReplicationSystem, CharacterReplicationEntry
├── Input/            # CharacterInputBuffer, CharacterInputValidator
├── Prediction/       # CharacterPredictionController
├── Interpolation/    # IEntitySnapshot, SnapshotBuffer, CharacterSnapshot, CharacterSnapshotInterpolator
├── Presentation/     # CharacterPresentationAdapter
└── TestSimulator/    # MultiPlayManager, ClientSimulator, ServerSimulator, TestPlayerSpawner
```

- `NetworkTimeSystem` broadcasts the shared fixed tick without requiring a net session.
- `NetworkObjectIdentity` implements `IEntityObjectIdentity` and registers with `CharacterReplicationSystem`.
- `EntitySimulationMode` is Authority, Predict, Replica, LocalPlay.
- Replica collision: `TransformModule.SetReplicaMode(true)` disables `CharacterController` and adds a capsule collider. `CharacterPresentationAdapter.ApplyRole` does not toggle the controller.
- Snapshots include `Character_Snapshot.view_rotation`. Interpolation slerps view pose; prediction reconcile includes view angle against the body-yaw thresholds. `rotation` is mesh body yaw; root rotation stays identity. When `presentationRoot` is `mesh`, prediction correction must not overwrite that yaw.

## Role Flow

```text
NetworkTimeSystem.Tick
        |
        v
CharacterReplicationSystem.AdvanceTick
  LocalPlay / Predict / Authority -> EntityCharacter.Step
  Replica -> interpolation only
```

Register APIs: `Register` and `RegisterLocalPlay`. LocalPlay ticks when `NetClient` and `NetServer` are both null.

## Protocol and Proxy Boundaries

Protocol remains under `Assets/Scripts/GamePlay/Protocol/Generated/`; generated classes keep the `NetSync` namespace. Proxy remains under `Assets/Scripts/GamePlay/Proxy/`.

## Dependency Direction

```text
Framework
   ↑
Network (transport and dispatch)
   ↑
MultiPlaySystem (role-aware gameplay sync)
   ↑
EntitySystem (consumed simulation/state contracts)
```

EntitySystem has no dependency on MultiPlaySystem.
