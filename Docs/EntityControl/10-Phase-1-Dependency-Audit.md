# Phase 1 Dependency Audit

This audit records the current direct coupling paths before runtime refactoring.

Scope: entity, controller, module, and network sync code under `Assets/Scripts/GamePlay`.

## Entity Core

`EntityCharacter` is the current central runtime type.

Direct responsibilities:

- Owns `EntityConfig` and creates `EntityContext`.
- Locates Unity components: `CharacterController`, `Animator`, `orientation`, and `mesh`.
- Creates `EntityMotor`.
- Injects `EntityContext` into `EntityAnimator`.
- Registers all locomotion FSM states.
- Exposes control methods: `Move`, `Aim`, `Jump`, `StartSprint`, `StopSprint`, `ToggleRun`.
- Exposes simulation entry: `Simulate(float deltaTime)`.
- Applies root motion through `OnAnimatorMove`.
- Uses `tickDrive` to switch between Unity `Update` simulation and external tick simulation.

Current coupling risk:

- Entity lifecycle, control input, state machine setup, movement module setup, animation setup, and network tick mode all meet in one MonoBehaviour.
- `tickDrive` is public and is controlled externally by networking/role code.

## Entity Context And FSM

`EntityContext` aggregates nearly all runtime state required by locomotion states.

Direct dependencies:

- `ExtendableStateMachine<uint>`
- `CharacterController`
- `Animator`
- `EntityConfig`
- `EntityMotor`
- input state fields: `LastMoveInput`, `LastAimInput`
- frame flags: `RunToggleRequest`, `DashRequest`, `JumpRequest`
- persistent movement flags: `IsSprinting`, `IsRunning`, `IsFocus`
- locomotion output field: `locomotionSpeed`

FSM dependency pattern:

- `EntityBaseState` stores `EntityContext`.
- `EntityLocomotionState` drives `EntityMotor.Rotate`, `EntityMotor.UpdateMeshFacing`, and `EntityMotor.Move`.
- Concrete states write `Context.locomotionSpeed` and read/write movement flags.
- `EntityDashState` exists but is currently not registered by `EntityCharacter`.

Current coupling risk:

- States depend on the concrete context instead of a narrow state data contract.
- Animation reads `EntityContext.locomotionSpeed` directly through `EntityAnimator`.
- Movement, state, animation parameter source, and input flags share the same mutable context.

## Movement And Animation Submodules

`EntityMotor` is a plain C# movement executor.

Direct dependencies:

- `CharacterController`
- `EntityConfig`
- `Transform orientation`
- `Transform mesh`

Main responsibilities:

- movement direction calculation
- gravity
- jump count
- dash state
- root motion displacement
- orientation and mesh facing

`EntityAnimator` is a MonoBehaviour animation adapter.

Direct dependencies:

- `Animator`
- `EntityContext`

Current coupling risk:

- `EntityMotor` already behaves like a movement module but is constructed by `EntityCharacter` and stored in `EntityContext`.
- `EntityAnimator` reads context directly instead of using a detachable animation module contract.

## Controller Layer

`LocalController` directly requires and drives:

- `EntityCharacter`
- `NetPositionSync`
- `IInputStateProvider`
- `SyncConfig`
- `CameraManager`

Direct behavior:

- subscribes to local input events in local play mode.
- maps input events to `EntityCharacter` methods.
- captures input for network prediction.
- calls `NetDriverInput.ApplyTo`.
- calls `_character.Simulate(tickTime)`.
- captures prediction snapshots with `_positionSync.CaptureSnapshot`.
- applies authority snapshots with `_positionSync.ApplySnapshot`.
- replays unconfirmed input by simulating `EntityCharacter` again.

`AuthorityController` directly requires and drives:

- `EntityCharacter`
- `NetPositionSync`
- queued `InputState`

Direct behavior:

- sets `_character.tickDrive = true`.
- consumes pending input.
- calls `NetDriverInput.ApplyTo`.
- calls `_character.Simulate(tickDeltaTime)`.
- captures authoritative snapshots through `NetPositionSync`.

`RemoteController` directly requires and drives:

- `NetEntityIdentity`
- `NetPositionSync`
- `EntityCharacter`

Direct behavior:

- validates replica role and entity id.
- forwards snapshots to `NetPositionSync`.
- advances interpolation through `NetPositionSync`.

`NetDriverInput` directly depends on:

- `EntityCharacter`
- `InputState`

Current coupling risk:

- Controllers are tied to concrete entity and concrete position sync component.
- Prediction, replay, input mapping, and simulation ownership are embedded in controllers.
- `NetDriverInput` maps network input to `EntityCharacter` instead of an intent/control contract.

## Network Sync Layer

`NetPositionSync` directly requires and uses:

- `NetEntityIdentity`
- `EntityCharacter`
- `CharacterController`
- `SnapshotBuffer<NetTransformSnapshot>`
- `SyncConfig`
- runtime-created `CapsuleCollider` for replicas

Direct behavior:

- role configuration enables/disables `CharacterController`.
- replica setup adds/enables a `CapsuleCollider`.
- captures transform snapshots from `transform`.
- reads movement state from `_character.CurrentState`.
- applies authoritative/interpolated transform state.
- stores render velocity and movement state.

`NetEntityRoleAssembler` directly uses:

- `NetEntityIdentity`
- `EntityCharacter`
- all child `INetSyncComponent` instances

Direct behavior:

- propagates role to sync components.
- sets `_character.tickDrive = role != NetEntityRole.LocalPlay`.

`INetSyncComponent` currently exposes:

- `SyncModuleID ModuleId`
- `ConfigureRole(NetEntityRole role)`

Current coupling risk:

- Position sync owns physics proxy behavior and transform state flow together.
- Sync reads entity state from `EntityCharacter`.
- Role assembly mutates entity simulation mode directly.
- Sync module interface does not yet separate data capture, role policy, interpolation, prediction, or replay.

## Immediate Refactor Implications

Phase 1 should reserve contracts before moving behavior:

- Entity simulation should be hidden behind a narrow interface before controllers are renamed or split.
- Input mapping should target an entity intent interface instead of `EntityCharacter`.
- Movement state read access should be exposed through an entity state view before sync is split.
- Role-driven tick mode should be moved behind an entity lifecycle/control policy contract.
- `EntityContext` should remain as a compatibility data container until the FSM gets a narrower state data adapter.
- `EntityMotor` and `EntityAnimator` are candidates for module wrapping before their internals are rewritten.

## Files Inspected

- `Assets/Scripts/GamePlay/EntitySystem/Character/EntityCharacter.cs`
- `Assets/Scripts/GamePlay/EntitySystem/EntityContext.cs`
- `Assets/Scripts/GamePlay/EntitySystem/SubModule/EntityMotor.cs`
- `Assets/Scripts/GamePlay/EntitySystem/SubModule/EntityAnimator.cs`
- `Assets/Scripts/GamePlay/EntitySystem/Controller/LocalController.cs`
- `Assets/Scripts/GamePlay/EntitySystem/Controller/AuthorityController.cs`
- `Assets/Scripts/GamePlay/EntitySystem/Controller/RemoteController.cs`
- `Assets/Scripts/GamePlay/EntitySystem/Controller/NetDriverInput.cs`
- `Assets/Scripts/GamePlay/EntitySystem/FSM/BaseState/EntityBaseState.cs`
- `Assets/Scripts/GamePlay/EntitySystem/FSM/BaseState/EntityLocomotionState.cs`
- `Assets/Scripts/GamePlay/EntitySystem/FSM/EntityState/EntityIdleState.cs`
- `Assets/Scripts/GamePlay/EntitySystem/FSM/EntityState/EntityWalkState.cs`
- `Assets/Scripts/GamePlay/EntitySystem/FSM/EntityState/EntityRunState.cs`
- `Assets/Scripts/GamePlay/EntitySystem/FSM/EntityState/EntitySprintState.cs`
- `Assets/Scripts/GamePlay/EntitySystem/FSM/EntityState/EntityAirborneState.cs`
- `Assets/Scripts/GamePlay/EntitySystem/FSM/EntityState/EntityDashState.cs`
- `Assets/Scripts/GamePlay/MultiPlaySystem/Component/NetPositionSync.cs`
- `Assets/Scripts/GamePlay/MultiPlaySystem/Component/NetEntityIdentity.cs`
- `Assets/Scripts/GamePlay/MultiPlaySystem/Component/NetEntityRoleAssembler.cs`
- `Assets/Scripts/GamePlay/MultiPlaySystem/Component/INetSyncComponent.cs`
- `Assets/Scripts/GamePlay/MultiPlaySystem/Component/SyncModuleID.cs`
- `Assets/Scripts/GamePlay/EntitySystem/Config/EntityConfig.cs`

