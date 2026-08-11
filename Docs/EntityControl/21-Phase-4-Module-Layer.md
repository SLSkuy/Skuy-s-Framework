# Phase 4 Module Layer

Phase 4 goal: move entity capabilities toward attachable modules while keeping entity core thin.

## Added

Location:

`Assets/Scripts/GamePlay/EntityControl/Modules`

Types:

- `EntityModuleBase`
- `MovementModule`
- `AnimationModule`
- `InteractionModule`
- `HealthModule`
- `CameraTargetModule`
- `SkillModule`
- `PhysicsProxyModule`

## Direct Replacements

Reusable old implementations were moved directly into the new module area:

- `EntityMotor`
- `EntityAnimator`

These files are not inheritance wrappers. They remain the reusable runtime implementations while the new module entries define how future entities will assemble movement and animation capabilities.

## Module Lifecycle

`EntityModuleBase` centralizes:

- `Target`
- `HasTarget`
- `IsEnabled`
- `Bind(IEntityControlTarget target)`
- `Unbind()`
- `SetEnabled(bool isEnabled)`
- `OnBound`
- `OnUnbound`
- `OnEnabledChanged`

This lets entity roles enable, disable, or swap abilities without adding behavior directly to `BaseEntity`.

## Capability Entries

`MovementModule` binds an `EntityMotor` instance as the movement execution backend.

`AnimationModule` reads `IEntityControlTarget.LocomotionSpeed` and optional `EntityConfig.animSpeedSmoothTime`; it updates Animator parameters but does not own state changes.

`SkillModule` implements `IEntitySkillEntry` and binds skills to an entity target rather than to a concrete controller.

`InteractionModule`, `HealthModule`, `CameraTargetModule`, and `PhysicsProxyModule` currently provide stable attachment points for the later gameplay and sync phases.

## Deferred

- Wire modules onto prefabs after component composition is rebuilt.
- Move remaining behavior out of heavy controllers after sync boundaries are ready.
- Expand health, interaction, camera target, skill, and physics proxy behavior when their owning phases need concrete logic.
- Run Unity Editor compile validation after Unity is available.
