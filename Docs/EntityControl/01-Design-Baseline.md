# EntityControl Design Baseline

This is the long-term baseline for entity control and network sync.

## Goal

Split the current entity stack into:
- base entity layer
- derived entity layer
- controller layer
- ability module layer
- sync layer

The current refactor focuses on entity structure, modular ability assembly, and network synchronization.

## Core Principle

- The base entity is thin.
- Controllers only own control sources and intent routing.
- Modules own concrete abilities.
- Sync owns cross-peer state flow.

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

Transform sync and animation sync should be modular and independently evolvable.
