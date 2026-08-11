# Phase 1 Target Layout

This document confirms the target folder layout for the EntityControl refactor.

Runtime root:

`Assets/Scripts/GamePlay/EntityControl`

## Target Runtime Folders

| Folder | Purpose |
| --- | --- |
| `Core/` | Entity-facing contracts, grouped under `Contracts`. |
| `Entities/` | Entity runtime grouped into `Core`, `Types`, `Config`, and `FSM`. |
| `Controllers/` | Control-source orchestrators grouped into `Contracts`, `Base`, `Input`, `Player`, `Authority`, `Replica`, and `AI`. |
| `Modules/` | Attachable ability modules grouped into `Core`, `Movement`, `Animation`, and `Gameplay`. |
| `Sync/` | Sync contracts and adapters for role dispatch, state capture, prediction, replay, interpolation, and animation sync. Split into `Core`, `Config`, `Snapshot`, `Timing`, `Transform`, `Animation`, and `Prediction`. |

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
| `Sync/Core/Contracts/` | sync component, snapshot source/receiver, interpolation, and registry contracts. |
| `Sync/Core/Identity/` | `NetEntityIdentity`, `NetEntityRole`, and role assembler. |
| `Sync/Core/Registry/` | `SyncModuleID` and sync module registry. |
| `Sync/Config/` | sync configuration assets. |
| `Sync/Snapshot/` | snapshot contracts, snapshot data, and snapshot buffering. |
| `Sync/Timing/` | fixed tick driving utilities. |

`Assets/Scripts/Utils` remains the home for global static helpers. Sync conversion helpers such as `NetSyncUtils` stay there until a later reviewed step decides whether they should become a sync adapter.

## Deferred Cleanup

`Assets/Scripts/GamePlay/EntitySystem/EntityControl` is not the target folder. Cleanup removes it after Unity references and meta state are checked.
