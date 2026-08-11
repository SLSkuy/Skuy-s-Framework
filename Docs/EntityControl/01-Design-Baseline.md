# EntityControl Design Baseline

This is the long-term baseline for entity control, skill integration, and network sync.

## Goal

Split the current entity stack into:
- base entity layer
- derived entity layer
- controller layer
- ability module layer
- sync layer
- skill entry layer

The system should stay extensible for animation, combat, interaction, vehicles, AI, and networking.

## Core Principle

- The base entity is thin.
- Controllers only own control sources and intent routing.
- Modules own concrete abilities.
- Sync owns cross-peer state flow.
- Skills plug into the entity, not into the base entity core.

## Recommended Entity Types

- `BaseEntity`
- `PlayerEntity`
- `RemotePlayerEntity`
- `NpcEntity`
- `MonsterEntity` or `BossEntity`
- `CompanionEntity`
- `VehicleEntity`
- `InteractableEntity`

## Recommended Modules

- `MovementModule`
- `AnimationModule`
- `SkillModule`
- `HealthModule`
- `InteractionModule`
- `InventoryModule`
- `CameraTargetModule`
- `PhysicsProxyModule`

## Recommended Controllers

- `PlayerController`
- `AuthorityController`
- `ReplicaController`
- `AIController`

## Network Sync Position

Sync should be an external capability layer, not a property of the entity core.

Position sync, animation sync, and skill sync should all be modular and independently evolvable.
