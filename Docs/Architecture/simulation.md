# Entity Simulation and MultiPlay Synchronization

The gameplay runtime is split into two folders:

- `Assets/Scripts/GamePlay/EntitySystem/` owns entity simulation and state.
- `Assets/Scripts/GamePlay/MultiPlaySystem/` owns network time, role selection, replication, prediction, interpolation, identity, and the test simulator.

`Protocol` and `Proxy` remain independent gameplay folders.

## Data Sources

- Input comes from `IInputStateProvider` and is converted by `EntityInputCommandBuilder`.
- Simulation state comes from `EntitySimulationState` and `EntityRollbackState`.
- Network input and snapshots use the generated `NetSync` protobuf messages under `GamePlay/Protocol/Generated`.
- Tick timing comes from `MultiPlaySystem.NetworkTimeSystem` and `SyncConfig`.

## Shared Simulation Path

```text
NetworkTimeSystem.Tick
        |
        v
CharacterReplicationSystem.AdvanceTick
  LocalPlay -> PlayerController.SampleInput -> EntityCharacter.Step
  Predict   -> local input -> EntityCharacter.Step -> history/reconciliation
  Authority -> validated queue -> EntityCharacter.Step
  Replica   -> no Step -> snapshot interpolation/presentation
```

`GameCore` registers `NetworkTimeSystem` and `CharacterReplicationSystem`. LocalPlay does not require `MultiPlayManager`, `NetClient`, or `NetServer`. Scene identities with `EntitySimulationMode.LocalPlay` register even when `NetworkObjectId` is 0.

Replica never calls `EntitySimulation.Step`. `TransformModule.SetReplicaMode` is the only place that disables the driving `CharacterController` for Replica.

`EntitySimulation.Step` order is `ViewModule.Look` -> `TransformModule.Rotate` -> `ViewModule.ApplyWorldPose` -> locomotion `TransformModule.Move`. Root rotation stays identity. `Rotate` turns child `mesh` toward the orientation-mapped move vector. `Move` translates along current mesh planar forward. `ApplyWorldPose` writes `orientation` as `Euler(pitch, yaw, 0)`.

## Prediction Contract

The owning client builds a command for each local tick, applies it immediately through `EntityCharacter.Step`, records rollback state, and sends `NetSync.Player_Input`. When `NetSync.Character_Snapshot` confirms an input tick, the client either removes confirmed history or restores the authoritative state and replays later commands through the same `Step` method.

## Snapshot Contract

The server captures authority state into `NetSync.Character_Snapshot` messages batched by `NetSync.World_Snapshot`. A non-owning client stores snapshots in MultiPlay `SnapshotBuffer<CharacterSnapshot>` and applies interpolated state through `CharacterPresentationAdapter`; it never calls `Step` for a Replica.

## Assembly and Protocol Boundary

No gameplay `.asmdef` is introduced. Generated Protocol files are not hand-edited, and the generated protobuf namespace remains `NetSync`.
