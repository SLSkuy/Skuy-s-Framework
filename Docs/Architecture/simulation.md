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
  LocalPlay -> local input -> EntitySimulation.Step
  Predict   -> local input -> EntitySimulation.Step -> history/reconciliation
  Authority -> validated queue -> EntitySimulation.Step
  Replica   -> no simulation -> snapshot interpolation/presentation
```

There is one role-aware scheduler. `EntitySimulationSystem` was removed after its only LocalPlay call chain moved into `CharacterReplicationSystem`.

## Prediction Contract

The owning client builds a command for each local tick, applies it immediately through `EntitySimulation.Step`, records rollback state, and sends `NetSync.Player_Input`. When `NetSync.Character_Snapshot` confirms an input tick, the client either removes confirmed history or restores the authoritative state and replays later commands through the same `Step` method.

## Snapshot Contract

The server captures authority state into `NetSync.Character_Snapshot` messages batched by `NetSync.World_Snapshot`. A non-owning client stores snapshots in `SnapshotBuffer` and applies interpolated state through `CharacterPresentationAdapter`; it never calls `EntitySimulation.Step` for a Replica.

## Assembly and Protocol Boundary

The obsolete `GamePlay.EntitySimulationCore.asmdef` is removed. No replacement gameplay assembly definition is introduced. Generated Protocol files are not hand-edited, and the generated protobuf namespace remains `NetSync` for compatibility.
