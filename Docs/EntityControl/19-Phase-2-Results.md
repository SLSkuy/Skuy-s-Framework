# Phase 2 Results

Phase 2 goal: split `EntityCharacter` toward a thin base entity plus derived entity types.

## Completed

- Added `BaseEntity` as the shared entity base and `IEntityControlTarget` implementation point.
- Kept `EntityCharacter` as the current compatibility behavior path.
- Added derived entity role shells:
  - `PlayerEntity`
  - `RemotePlayerEntity`
  - `NpcEntity`
  - `MonsterEntity`
  - `BossEntity`
  - `CompanionEntity`
  - `VehicleEntity`
  - `InteractableEntity`
- Moved direct `EntityContext` access behind entity-layer methods:
  - `TryGetConfig`
  - `TryGetContext`
  - `SetContext`
  - `ClearContext`
- Confirmed current Prefab component deletion is intentional and will be handled after the new assembly path is ready.

## Static Validation

Completed:

- Checked for typical Unity missing-script markers in `Assets`.
- Confirmed derived entity type definitions exist.
- Confirmed `EntityCharacter` still wraps the public `tickDrive` compatibility field.
- Confirmed `EntityCharacter` context creation now goes through `SetContext`.
- Confirmed no external `.Context` property usage remains.

## Compile Validation

Unity compile validation is still required in the Editor.

The command environment still does not expose:

- `Unity`
- `dotnet`
- `msbuild`
- `csc`

## Deferred To Later Phases

- Re-add runtime components to prefabs after new entity/controller/sync structure is ready.
- Move controllers from concrete `EntityCharacter` references to `IEntityControlTarget`.
- Split `EntityCharacter` behavior into modules.
- Replace `EntityContext` with narrower state/movement adapters.

