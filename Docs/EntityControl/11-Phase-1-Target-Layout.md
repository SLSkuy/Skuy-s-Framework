# Phase 1 Target Layout

This document confirms the target folder layout for the EntityControl refactor.

Runtime root:

`Assets/Scripts/GamePlay/EntityControl`

## Target Runtime Folders

| Folder | Purpose |
| --- | --- |
| `Core/` | Narrow contracts, base lifecycle seams, shared state views, and compatibility adapters. |
| `Entities/` | `BaseEntity` and derived entity types such as `PlayerEntity`, `RemotePlayerEntity`, `NpcEntity`, `MonsterEntity`, `BossEntity`, `CompanionEntity`, `VehicleEntity`, and `InteractableEntity`. |
| `Controllers/` | Control-source orchestrators such as player, authority, replica, and AI controllers. |
| `Modules/` | Attachable ability modules such as movement, animation, interaction, health, camera target, and physics proxy. |
| `Sync/` | Sync contracts and adapters for role dispatch, state capture, prediction, replay, interpolation, and animation sync. Split into `Core`, `Config`, `Snapshot`, and `Timing`. |

## Namespace Decision

For Phase 1, new runtime code should keep the existing namespace:

`GamePlay.EntitySystem`

Reason: the project currently compiles into one `Assembly-CSharp` assembly and existing entity/runtime types already use this namespace. Keeping it avoids a wide namespace churn before behavior is split.

## Compatibility Sources

Existing behavior remains in place until each phase migrates it:

- `Assets/Scripts/GamePlay/EntitySystem/Character`
- `Assets/Scripts/GamePlay/EntitySystem/FSM`
- `Assets/Scripts/GamePlay/MultiPlaySystem/Component`

## Sync Folder Split

| Folder | Purpose |
| --- | --- |
| `Sync/Core/` | `NetEntityRole`, `SyncModuleID`, and sync component contracts. |
| `Sync/Config/` | sync configuration assets. |
| `Sync/Snapshot/` | snapshot contracts, snapshot data, and snapshot buffering. |
| `Sync/Timing/` | fixed tick driving utilities. |

`Assets/Scripts/Utils` remains the home for global static helpers. Sync conversion helpers such as `NetSyncUtils` stay there until a later reviewed step decides whether they should become a sync adapter.

## Deferred Cleanup

`Assets/Scripts/GamePlay/EntitySystem/EntityControl` is not the target folder. Cleanup removes it after Unity references and meta state are checked.
