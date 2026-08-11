# Phase 5 Network Sync

Phase 5 goal: make synchronization an external module layer for state transfer, role dispatch, prediction, interpolation, and animation sync.

## Added

Core:

- `INetInterpolatedSync<TSnapshot>`
- `INetSyncModuleRegistry`
- `NetSyncModuleRegistry`

Prediction:

- `INetPredictionModule<TSnapshot>`
- `INetReplayModule`

Animation:

- `NetAnimationSnapshot`
- `NetAnimationSync`

Transform:

- `NetTransformSync`

## Direct Replacement

`NetPositionSync` was moved out of `MultiPlaySystem/Component` and directly replaced by `NetTransformSync` under:

`Assets/Scripts/GamePlay/EntityControl/Sync/Transform`

The old class name is not retained as an inheritance compatibility wrapper.

## Registry And Role Dispatch

`NetSyncModuleRegistry` scans child `INetSyncComponent` modules and provides:

- module list access
- `SyncModuleID` lookup
- typed module lookup

`NetEntityRoleAssembler` now uses the registry for role dispatch and remains responsible for applying `NetEntityRole` to all sync modules on the entity.

## Sync Module IDs

`SyncModuleID` now defines:

- `Transform`
- `Animation`

## Deferred

- Move prediction/replay implementation out of `PlayerController` after the next behavior-cleanup pass.
- Move authority simulation queue ownership out of `AuthorityController` when server simulation is normalized.
- Connect animation snapshots to protocol messages after entity sync contracts are stable.
- Re-add prefab sync components after the new entity/controller/sync assembly path is ready.
- Run Unity Editor compile validation after Unity is available.
