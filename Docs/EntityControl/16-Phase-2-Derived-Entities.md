# Phase 2 Derived Entities

Task 2.2 defines the first derived entity type set.

## Added

Location:

`Assets/Scripts/GamePlay/EntityControl/Entities`

Types:

- `PlayerEntity`
- `RemotePlayerEntity`
- `NpcEntity`
- `MonsterEntity`
- `BossEntity`
- `CompanionEntity`
- `VehicleEntity`
- `InteractableEntity`

## Compatibility Strategy

All derived entity types currently inherit from `EntityCharacter`.

Reason: `EntityCharacter` still owns the current behavior path, including FSM setup, context creation, movement simulation, root motion, and input write methods. Keeping these derived types as behavior shells avoids duplicating or moving gameplay behavior during task 2.2.

This also keeps existing prefab and controller flows stable because current prefabs still use `EntityCharacter` directly.

## Type Meaning

| Type | Role |
| --- | --- |
| `PlayerEntity` | Local player entity entry. |
| `RemotePlayerEntity` | Remote player snapshot/render entry. |
| `NpcEntity` | Non-player character entry. |
| `MonsterEntity` | Enemy unit entry. |
| `BossEntity` | High-level enemy unit entry. |
| `CompanionEntity` | Follower, companion, or summoned unit entry. |
| `VehicleEntity` | Vehicle control entry. |
| `InteractableEntity` | Scene interaction object entry. |

## Deferred

- Move role-specific behavior out of `EntityCharacter`.
- Decide whether all derived entity types should require `CharacterController`.
- Migrate prefabs from `EntityCharacter` to derived entity scripts.
- Bind controllers to `IEntityControlTarget` instead of concrete `EntityCharacter`.

