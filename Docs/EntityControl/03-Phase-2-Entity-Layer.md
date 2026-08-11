# Phase 2: Entity Layer

## Goal

Split `EntityCharacter` into a thin base entity plus derived entity types.

## Tasks

- define `BaseEntity`
- define entity identity and lifecycle boundaries
- define derived entity types
- define component lookup and module access
- define default entity configs

## Suggested Entity Types

- `BaseEntity`
- `PlayerEntity`
- `RemotePlayerEntity`
- `NpcEntity`
- `MonsterEntity`
- `BossEntity`
- `CompanionEntity`
- `VehicleEntity`
- `InteractableEntity`

## Success Criteria

- the base entity is thin
- derived entities reflect control style and gameplay role
- `EntityCharacter` no longer carries all responsibilities

