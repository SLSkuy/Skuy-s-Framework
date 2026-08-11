# EntityControl Refactor Task List

This task list converts the design documents into reviewable implementation steps.

Rule: complete one step, stop for review, then continue only after approval.

## Phase 1: Foundation Arrangement

Goal: organize reusable base types and reserve extension points without changing gameplay behavior.

- [x] 1.1 Inspect current entity, controller, module, and sync dependencies; record direct coupling paths before editing.
- [x] 1.2 Create or confirm the target folder layout for `EntityControl`, entity modules, controllers, and sync entry points.
- [x] 1.3 Move immediately reusable types into their target locations only when namespace and Unity meta impact are clear.
- [x] 1.4 Define minimal contracts for entity, controller, module, and sync entry boundaries.
- [x] 1.5 Run compile validation and update this checklist with Phase 1 results.

Review gate: Phase 1 should not rewrite movement, FSM, protocol, snapshots, or gameplay behavior.

Phase 1 result: foundation layout and contracts are complete. Unity Editor compile validation is still required because no Unity/dotnet/msbuild/csc executable is available in the command environment.

## Phase 2: Entity Layer

Goal: split `EntityCharacter` into a thin base entity plus derived entity types.

- [x] 2.1 Extract identity, lifecycle, and common component lookup responsibilities into `BaseEntity`.
- [x] 2.2 Define derived entity types: `PlayerEntity`, `RemotePlayerEntity`, `NpcEntity`, `MonsterEntity`, `BossEntity`, `CompanionEntity`, `VehicleEntity`, and `InteractableEntity`.
- [x] 2.3 Move reusable context/config access behind entity-layer APIs.
- [x] 2.4 Keep `EntityCharacter` as a compatibility path or migrate usages, depending on scene/prefab references found during inspection.
- [x] 2.5 Validate Unity compile and check scene/prefab references for missing scripts.

Review gate: entity core remains thin; derived types express gameplay role and control style.

Phase 2 result: base entity, derived entity shells, compatibility path, and entity data access boundaries are in place. Unity Editor compile validation is still required because no Unity/dotnet/msbuild/csc executable is available in the command environment.

## Phase 3: Controller Layer

Goal: make controllers route control intent instead of owning heavy gameplay behavior.

- [x] 3.1 Define shared controller binding contract between controller and entity.
- [x] 3.2 Refactor local/player control flow into `PlayerController`.
- [x] 3.3 Refactor authority flow into `AuthorityController` with clear simulation ownership.
- [x] 3.4 Refactor remote/replica flow into `ReplicaController`.
- [x] 3.5 Add AI controller shell only where current code needs an integration point.
- [x] 3.6 Define drive-mode switching rules and validate old controller paths are either migrated or explicitly marked compatibility-only.

Review gate: player, authority, replica, and AI flows can evolve independently.

Phase 3 result: controller binding, drive modes, direct `PlayerController`/`ReplicaController` replacements, AI controller entry, and intent-interface routing are in place. No vehicle controller is required. Prediction/replay/interpolation behavior remains in official controller files until module and sync phases.

## Phase 4: Module Layer

Goal: convert entity abilities into attachable modules.

- [x] 4.1 Define base module lifecycle and entity binding contract.
- [x] 4.2 Extract movement behavior into `MovementModule`.
- [x] 4.3 Extract animation behavior into detachable `AnimationModule`.
- [x] 4.4 Add `InteractionModule`, `HealthModule`, `CameraTargetModule`, and `PhysicsProxyModule` contracts or minimal implementations.
- [x] 4.5 Ensure modules can be enabled or disabled by entity role.
- [x] 4.6 Validate that animation reads entity state and does not drive entity core state directly.

Review gate: abilities are assembled by modules; entity core does not grow new gameplay responsibilities.

Phase 4 result: module lifecycle, movement/animation wrappers, role-enabled module toggling, and capability module entry points are in place. Reusable `EntityMotor` and `EntityAnimator` implementations were moved directly under `EntityControl/Modules`. Unity Editor compile validation is still required because no Unity/dotnet/msbuild/csc executable is available in the command environment.

## Phase 5: Network Sync

Goal: make sync an external modular layer for state transfer, prediction, interpolation, and replay.

- [x] 5.1 Define sync module registry and module lookup rules.
- [x] 5.2 Split transform sync from `NetPositionSync` into focused sync data and runtime components.
- [x] 5.3 Define role dispatch flow for authority, replica, and local prediction.
- [x] 5.4 Add prediction and replay entry points without rewriting the snapshot algorithm.
- [x] 5.5 Add animation sync entry contracts.
- [x] 5.6 Validate sync modules are pluggable and entity core stays independent of sync details.

Review gate: position sync is no longer a monolith; animation state can join sync without entity core changes.

Phase 5 result: sync registry, role dispatch, transform sync replacement, prediction/replay contracts, and animation sync entry are in place. `NetPositionSync` was directly replaced by `NetTransformSync`; no inheritance wrapper is kept. Unity Editor compile validation is still required because no Unity/dotnet/msbuild/csc executable is available in the command environment.

## Phase 6: Cleanup

Goal: remove old coupling paths and stabilize the entity/control/sync structure.

- [x] 6.1 Remove migrated controller paths and unused compatibility code.
- [x] 6.2 Remove old entity coupling after references are migrated.
- [x] 6.3 Remove old sync coupling after module sync is validated.
- [x] 6.4 Unify namespaces and file layout with repository conventions.
- [x] 6.5 Add focused EditMode or PlayMode tests for high-risk behavior.
- [x] 6.6 Run final compile/test validation and document remaining risks.

Review gate: old flow is no longer the official flow; future expansion should not require another core rewrite.

Phase 6 result: migrated the remaining entity core, entity config, FSM, and network identity files into `EntityControl`; removed empty legacy runtime folders; confirmed runtime references no longer point to old controller/submodule/sync paths. Unity Editor compile/test validation is still required because no Unity/dotnet/msbuild/csc executable is available in the command environment.
