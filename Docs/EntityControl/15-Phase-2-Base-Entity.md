# Phase 2 Base Entity

Task 2.1 extracts identity, lifecycle, and common component lookup responsibilities into `BaseEntity`.

## Added

Runtime file:

`Assets/Scripts/GamePlay/EntityControl/Entities/BaseEntity.cs`

`BaseEntity` is an abstract `MonoBehaviour` and implements `IEntityControlTarget`.

It now owns the shared entity references:

- `EntityConfig`
- `EntityContext`
- `CharacterController`
- `Animator`
- `orientation` transform
- `mesh` transform
- optional `NetEntityIdentity`

It also exposes read-only accessors for common entity identity and cached component data:

- `Config`
- `Context`
- `CharacterController`
- `Animator`
- `Orientation`
- `Mesh`
- `Identity`
- `EntityId`
- `HasIdentity`

## Compatibility Path

`EntityCharacter` now inherits from `BaseEntity`.

The existing `EntityCharacter` script remains the component attached to prefabs and scenes. This keeps the current Unity script GUID and avoids missing-script risk during the first entity-layer step.

The existing public `tickDrive` field remains in `EntityCharacter` for current controller and role assembler code. `EntityCharacter.TickDrive` wraps that field to satisfy the new `IEntitySimulation` contract.

## Behavior Scope

This step does not change:

- FSM state registration
- movement simulation
- input writing
- root motion behavior
- controller behavior
- network sync behavior

`EntityCharacter` still owns the current behavior path. `BaseEntity` only provides shared lifecycle/component boundaries for later derived entity types.

## Deferred

- Moving FSM registration out of `EntityCharacter`.
- Replacing `EntityContext` with a narrower state adapter.
- Updating controllers to depend on `IEntityControlTarget`.
- Migrating prefab components to derived entity types.

