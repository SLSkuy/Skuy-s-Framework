# Phase 2 Entity Data Access

Task 2.3 moves reusable context/config access behind entity-layer APIs.

## Updated

Runtime file:

`Assets/Scripts/GamePlay/EntityControl/Entities/BaseEntity.cs`

Changes:

- Removed direct public `Context` property exposure.
- Kept read-only `Config` property for simple configuration inspection.
- Added `IsInitialized` to indicate whether an entity runtime context has been assembled.
- Added `TryGetConfig(out EntityConfig config)`.
- Added `TryGetContext(out EntityContext context)`.
- Added protected `SetContext(EntityContext context)`.
- Added protected `ClearContext()`.

Runtime file:

`Assets/Scripts/GamePlay/EntitySystem/Character/EntityCharacter.cs`

Changes:

- Uses `SetContext(new EntityContext(...))` when assembling the current compatibility context.

## Boundary Intent

`EntityContext` remains available for compatibility with the current FSM and animation path, but it is no longer exposed as a casual public property from `BaseEntity`.

Future modules and controllers should prefer:

- `IEntityControlTarget`
- `IEntityStateView`
- `BaseEntity.TryGetConfig`
- `BaseEntity.TryGetContext` only when bridging old compatibility code

## Deferred

- Replacing `EntityContext` with narrower state and movement data adapters.
- Updating `EntityAnimator` to read from `IEntityStateView`.
- Updating controllers to avoid direct `EntityCharacter` references.

