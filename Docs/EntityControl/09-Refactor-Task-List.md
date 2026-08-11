# EntityControl Refactor Task List

This task list converts the design documents into reviewable implementation steps.

Rule: complete one step, stop for review, then continue only after approval.

## Phase 1: Foundation Arrangement

Goal: organize reusable base types and reserve extension points without changing gameplay behavior.

- [x] 1.1 Inspect current entity, controller, module, and sync dependencies; record direct coupling paths before editing.
- [x] 1.2 Create or confirm the target folder layout for `EntityControl`, entity modules, controllers, and sync entry points.
- [x] 1.3 Move immediately reusable types into their target locations only when namespace and Unity meta impact are clear.
- [x] 1.4 Define minimal contracts for entity, controller, module, skill entry, and sync entry boundaries.
- [ ] 1.5 Run compile validation and update this checklist with Phase 1 results.

Review gate: Phase 1 should not rewrite movement, FSM, protocol, snapshots, or gameplay behavior.

## Phase 2: Entity Layer

Goal: split `EntityCharacter` into a thin base entity plus derived entity types.

- [ ] 2.1 Extract identity, lifecycle, and common component lookup responsibilities into `BaseEntity`.
- [ ] 2.2 Define derived entity types: `PlayerEntity`, `RemotePlayerEntity`, `NpcEntity`, `MonsterEntity`, `BossEntity`, `CompanionEntity`, `VehicleEntity`, and `InteractableEntity`.
- [ ] 2.3 Move reusable context/config access behind entity-layer APIs.
- [ ] 2.4 Keep `EntityCharacter` as a compatibility path or migrate usages, depending on scene/prefab references found during inspection.
- [ ] 2.5 Validate Unity compile and check scene/prefab references for missing scripts.

Review gate: entity core remains thin; derived types express gameplay role and control style.

## Phase 3: Controller Layer

Goal: make controllers route control intent instead of owning heavy gameplay behavior.

- [ ] 3.1 Define shared controller binding contract between controller and entity.
- [ ] 3.2 Refactor local/player control flow into `PlayerController`.
- [ ] 3.3 Refactor authority flow into `AuthorityController` with clear simulation ownership.
- [ ] 3.4 Refactor remote/replica flow into `ReplicaController`.
- [ ] 3.5 Add AI and vehicle controller shells only where current code needs integration points.
- [ ] 3.6 Define drive-mode switching rules and validate old controller paths are either migrated or explicitly marked compatibility-only.

Review gate: player, authority, replica, and AI flows can evolve independently.

## Phase 4: Module Layer

Goal: convert entity abilities into attachable modules.

- [ ] 4.1 Define base module lifecycle and entity binding contract.
- [ ] 4.2 Extract movement behavior into `MovementModule`.
- [ ] 4.3 Extract animation behavior into detachable `AnimationModule`.
- [ ] 4.4 Add `InteractionModule`, `HealthModule`, `CameraTargetModule`, `SkillModule`, and `PhysicsProxyModule` contracts or minimal implementations.
- [ ] 4.5 Ensure modules can be enabled or disabled by entity role.
- [ ] 4.6 Validate that animation reads entity state and does not drive entity core state directly.

Review gate: abilities are assembled by modules; entity core does not grow new gameplay responsibilities.

## Phase 5: Network Sync

Goal: make sync an external modular layer for state transfer, prediction, interpolation, and replay.

- [ ] 5.1 Define sync module registry and module lookup rules.
- [ ] 5.2 Split transform sync from `NetPositionSync` into focused sync data and runtime components.
- [ ] 5.3 Define role dispatch flow for authority, replica, and local prediction.
- [ ] 5.4 Add prediction and replay entry points without rewriting the snapshot algorithm.
- [ ] 5.5 Add animation sync and skill sync entry contracts.
- [ ] 5.6 Validate sync modules are pluggable and entity core stays independent of sync details.

Review gate: position sync is no longer a monolith; animation and skill state can join sync without entity core changes.

## Phase 6: Skill Integration

Goal: integrate skills as a first-class entity capability.

- [ ] 6.1 Define skill-facing entity contract.
- [ ] 6.2 Define skill configuration and runtime instance separation.
- [ ] 6.3 Add skill state machine contract.
- [ ] 6.4 Connect skill execution to animation through module contracts.
- [ ] 6.5 Connect skill state to sync through sync entry contracts.
- [ ] 6.6 Validate skills do not depend on controller internals.

Review gate: skills attach cleanly to entities and can run locally or through networking.

## Phase 7: Cleanup

Goal: remove old coupling paths and stabilize the new structure.

- [ ] 7.1 Remove migrated controller paths and unused compatibility code.
- [ ] 7.2 Remove old entity coupling after references are migrated.
- [ ] 7.3 Remove old sync coupling after module sync is validated.
- [ ] 7.4 Unify namespaces and file layout with repository conventions.
- [ ] 7.5 Add focused EditMode or PlayMode tests for high-risk behavior.
- [ ] 7.6 Run final compile/test validation and document remaining risks.

Review gate: old flow is no longer the official flow; future expansion should not require another core rewrite.
