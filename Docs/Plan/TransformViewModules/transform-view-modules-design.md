# Transform and View Modules

## Workflow Status
Plan Status: In Progress
Confirmation Gate: Confirmed
Confirmation Evidence: On 2026-08-24 the user confirmed three migration boundaries: (1) aim input is a per-tick delta and locomotion moves along view facing; (2) the snapshot protocol gains a view-rotation field; (3) `ViewModule` is added on the entity root and resolves the default child named `orientation` with `Transform.Find` during `Init`. On 2026-08-25 the user sent `/apply`, approving this written design for implementation. On 2026-08-25 the user further required root rotation to stay identity, body yaw on child `mesh`, orientation-relative input as the mesh turn target, and planar displacement along current mesh facing.

## Background and Goal

`MovementModule` and `RotationModule` both live on the entity root and split one rigid transform. Root rotation currently treats `EntityCommand.aim` as a planar facing vector, so the unused `orientation` child on `NetPlayer` never participates in simulation. `EntityConfig` already has look speeds and pitch limits with no caller.

The goal is one root transform capability plus a separate view capability that owns the look node, drives movement from look yaw, and replicates look pose to replicas.

Success is observable when:

- LocalPlay, Authority, and Predict characters translate along current **mesh** planar facing; WASD is mapped relative to `orientation` yaw to choose the mesh turn target.
- Look yaw/pitch accumulate from aim deltas, clamp to `minAimPitch` / `maxAimPitch`, and write the `orientation` child.
- Root rotation stays identity. `mesh` turns with `meshTurnSpeed` toward the orientation-mapped planar move vector while moving, and holds while idle.
- `Character_Snapshot` carries `view_rotation`. Replicas interpolate it. Predict reconciliation includes view error.
- `MovementModule` and `RotationModule` are removed.

## Scope

In scope:

- Merge `MovementModule` and `RotationModule` into `TransformModule` on the entity root.
- Add `ViewModule` on the same root. During `Init`, `transform.Find("orientation")` binds the look node.
- Drive `ViewModule` from aim deltas; map move input relative to orientation yaw; turn child `mesh`; translate along mesh facing.
- Extend `EntitySimulationState`, rollback capture/restore, `ProtoUtils`, interpolation, presentation apply, and prediction reconcile.
- Add `view_rotation` to `Character_Snapshot` and regenerate protobuf C# through `Tools/Protobuf/Proto Compiler`.
- Fix `EntityCharacter` so modules are created before `EntityContext` / `EntitySimulation`.
- Update `Docs/Architecture/entity.md`, `simulation.md`, and `networking.md`.

Out of scope:

- Camera follow, Cinemachine, or binding `Camera.main` to `orientation`.
- Changing `Player_Input` field layout. `aim_input` stays `Vec2`; only semantics change.
- Hand-editing `GamePlay/Protocol/Generated/`.
- New `.asmdef` files.
- Dash, focus, AI policy, or animation modules.
- Merging the parallel `StateMachine/` and `LocomotionStateMachine/` trees except to retarget module APIs they already call.
- Changing Network transport or tick ownership.

## Assumptions and Decisions

- Child name is `orientation` (matches `Assets/Resources/NetPlayer.prefab` and the user confirmation). `Find` is a direct-child lookup.
- Missing `orientation` fails loudly (`LogError` / early return). No runtime node creation.
- Aim.x increases yaw, aim.y increases pitch (implementation may negate Y so up-look matches the input asset). `yaw += aim.x * aimHorizontalSpeed * dt`, then clamp pitch with `minAimPitch` / `maxAimPitch`.
- Tick `dt` is applied so look is deterministic at `SyncConfig.simulationTickRate`. Mouse bindings that already emit per-frame pixels may need a later sensitivity pass; not this plan.
- `ViewModule` stores yaw/pitch independently and assigns **world** rotation on `orientation` as `Quaternion.Euler(pitch, yaw, 0)`. Root rotation stays identity so parent yaw cannot double-apply look.
- Move input is mapped relative to orientation yaw only to produce the mesh turn target. Planar displacement follows current mesh forward, not the orientation vector.
- Mesh yaw: `RotateTowards` the planar orientation-mapped move vector at `meshTurnSpeed` when move magnitude is above epsilon; otherwise keep last mesh yaw. Idle look-around does not spin the body. Root rotation remains identity.
- Network view pose is `orientation.rotation` (`viewRotation` on `EntitySimulationState`).
- Replica still never calls `EntitySimulation.Step`. Interpolation writes both root and view through presentation.
- Dash fields on movement rollback stay unused.
- `EntitySystem` still does not reference `MultiPlaySystem`.
- Init order becomes `InitConfig` -> `InitCapacityModule` -> `InitSimulationContext` -> `RegisterStates`.

## Repository Evidence

- Split modules: `Modules/Position/MovementModule.cs`, `Modules/Transform/RotationModule.cs`, `ModuleType.Position` / `Rotation`.
- Assembly: `EntityCharacter.InitCapacityModule` adds both on the root. `InitSimulationContext` currently runs first and constructs `EntityContext` with null module references.
- Step: `EntitySimulation.Step` calls `Rotation.Rotate(move, aim, dt)` then the locomotion state machine. Ground states call `Movement.Move(LastMoveInput, speed, dt)`, which treats input XZ as world axes (`UpdateLocomotionDir`).
- Look config unused: `EntityConfig.aimHorizontalSpeed`, `aimVerticalSpeed`, `minAimPitch`, `maxAimPitch`.
- Prefab: `NetPlayer` children `orientation` (y ≈ 1.45) and `mesh` (`presentationRoot` on `CharacterPresentationAdapter`).
- Snapshot: `EntitySimulationState` has one `rotation`. `Character_Snapshot` fields 1–10; `rotation = 5`. Mapping is `Utils.ProtoUtils`.
- MultiPlay apply: `CharacterPresentationAdapter.ApplyState` teleports movement and restores rotation. `CharacterReplicationEntry.ApplyRole` calls `MovementModule.SetReplicaMode`. `CharacterSnapshotInterpolator` lerps position and slerps root rotation only. Predict reconcile in `CharacterReplicationSystem` compares root position/rotation only.
- Archived consolidation (2026-08-23) kept split modules and a single snapshot rotation. It does not describe view replication.

No external engine reference is required; the prefab and unused aim config already encode the intended third-person split.

## Data Sources and Ownership

| Data | Owner | Source |
| --- | --- | --- |
| Move / aim vectors and buttons | `EntityCommand` | `PlayerController` + `EntityCommandBuilder`; wire `NetSync.Player_Input.aim_input` |
| Root position, velocities, grounded | `TransformModule` | `CharacterController` on the entity root; root rotation stays identity |
| Body yaw | `TransformModule` | Direct child `mesh` |
| View yaw/pitch | `ViewModule` | Aim delta + `EntityConfig` look fields; written to `orientation` |
| Network character pose | `EntitySimulationState` | `EntitySimulation.CaptureSimulationState` |
| Wire snapshot | `NetSync.Character_Snapshot` | `Assets/Scripts/GamePlay/Protocol/net_sync.proto` via `ProtoUtils` |
| Replica display | `CharacterPresentationAdapter` | Interpolated `EntitySimulationState` |
| Tick length | `SyncConfig.simulationTickRate` | `NetworkTimeSystem` |

## Selected Design

```text
EntityCommand.aim (delta)
        |
        v
EntitySimulation.Step
  1. ViewModule.Look(aim, dt)           -> orientation world rotation
  2. TransformModule.Rotate(move, dt)   -> mesh yaw toward orientation-mapped move
  3. StateMachine.Update                -> TransformModule.Move(move, speed, dt)
                                              planar speed along current mesh forward
```

**TransformModule** (root, `ModuleType.Transform`):

- Absorbs `MovementModule` (controller, replica collider, teleport, gravity, jump, dash storage, locomotion dir) and body yaw / angular velocity from `RotationModule`, written to child `mesh`.
- `Move` uses current mesh planar forward for `CharacterController.Move`; input magnitude scales speed. Input direction is not applied as a world slide.
- `Rotate` turns `mesh` toward the orientation-mapped planar move vector and never rotates the root.
- `SetReplicaMode` remains the only Replica controller disable path.

**ViewModule** (root, `ModuleType.View`):

- `Init` sets `_orientation = transform.Find("orientation")`.
- `Look` accumulates yaw/pitch, clamps pitch, writes `_orientation.rotation`.
- Capture/restore feed `viewRotation`. Replica apply uses `Restore`, not `Look`.

**State:**

- `EntitySimulationState.viewRotation`. Wire protocol is the quaternion only.
- `EntityRollbackState` gains `ViewRollbackState` with yaw, pitch, and view angular velocity so restore does not decode wrapped eulers.

**Protocol:**

- `Character_Snapshot.view_rotation = 11` type `Quat`.
- Regenerated C# with project `Protocol/protoc.exe` (same compiler as `Tools/Protobuf/Proto Compiler`). Do not edit `Generated/NetSync.cs` by hand.
- `Player_Input.aim_input` unchanged. Semantics become per-tick look delta.

**MultiPlay:**

- `ProtoUtils.ToCharacterSnapshotMessage` / `ToSimulationState` copy `viewRotation`.
- `CharacterSnapshotInterpolator` slerps `viewRotation`. Snap also when view angle exceeds `SyncConfig.rotationSnapThresholdDegrees` (reuse root thresholds unless playtest requires a dedicated field).
- `CharacterReplicationSystem` reconcile treats view angle like root rotation against `rotationReconcileThresholdDegrees` / `rotationSnapThresholdDegrees`.
- `CharacterPresentationAdapter.ApplyState` restores `TransformModule` then `ViewModule`.

**Naming / folders:**

- Add `Modules/Transform/TransformModule.cs` and `Modules/View/ViewModule.cs`.
- Delete `MovementModule`, `RotationModule`, and empty `Modules/Position/` after call sites move.
- `EntityContext.Transform` and `EntityContext.View` replace `Movement` / `Rotation`.
- Locomotion bases call `Transform.Move` / `Transform.Jump`.

## Modules and Files Affected

### EntitySystem

- Add: `Modules/Transform/TransformModule.cs`, `Modules/View/ViewModule.cs`, `Snapshot/State/ViewRollbackState.cs`
- Delete: `Modules/Position/MovementModule.cs`, `Modules/Transform/RotationModule.cs`
- Modify: `Modules/ModuleType.cs`, `Entities/EntityCharacter.cs`, `Entities/EntityContext.cs`, `Simulation/EntitySimulation.cs`
- Modify: `Snapshot/State/EntitySimulationState.cs`, `Snapshot/State/EntityRollbackState.cs`
- Modify: locomotion bases under `LocomotionStateMachine/` and any parallel `StateMachine/` files that still reference `MovementModule`

### MultiPlaySystem

- Modify: `Presentation/CharacterPresentationAdapter.cs`
- Modify: `Replication/CharacterReplicationEntry.cs`, `Replication/CharacterReplicationSystem.cs`
- Modify: `Interpolation/CharacterSnapshotInterpolator.cs`

### Protocol / Utils

- Modify: `Assets/Scripts/GamePlay/Protocol/net_sync.proto`
- Regenerate: `Assets/Scripts/GamePlay/Protocol/Generated/NetSync.cs`
- Modify: `Assets/Scripts/Utils/ProtoUtils.cs`

### Prefab / docs

- Prefab: no hierarchy change; `orientation` already exists. `GetOrAddComponent` adds the new modules at `Init`.
- Modify: `Docs/Architecture/entity.md`, `simulation.md`, `networking.md`
- This plan folder and `Docs/Plan/README.md`

`.asmdef`: none.

## Interface and Call-Chain Changes

- `EntityContext` constructor takes `TransformModule` and `ViewModule`.
- `ModuleType.Position` and `Rotation` removed; `Transform` and `View` added.
- `CharacterPresentationAdapter.ApplyState` depends on `TransformModule` + `ViewModule`.
- `CharacterReplicationEntry.ApplyRole` calls `TransformModule.SetReplicaMode`.
- `EntitySimulationState` gains `viewRotation`.
- `Character_Snapshot` field 11. Absent fields deserialize as identity (proto3).
- `.asmdef`: none.

```text
CharacterReplicationSystem tick
  -> EntityCharacter.Step -> EntitySimulation.Step
       View.Look -> Transform.Rotate -> View.ApplyWorldPose -> StateMachine.Update -> Transform.Move
  -> CaptureSimulationState (root + view)
  -> ProtoUtils.ToCharacterSnapshotMessage

Replica:
  World_Snapshot -> ToSimulationState -> interpolator -> ApplyState
       Transform.Teleport/Restore + View.Restore

View snapshot pose is `Quaternion.Euler(pitch, yaw, 0)` from stored yaw/pitch, not the child world rotation after root body yaw. After `Transform.Rotate`, `View.ApplyWorldPose` rewrites `orientation` so look basis and move yaw stay aligned.
```

Compatibility: in-development protocol; peers must regenerate together. Aim meaning changes without a `Player_Input` field bump.

## Lifecycle and Dependency Impact

- Module `Init`/`Bind` stay on `EntityCharacter.Init`, invoked from `CharacterReplicationEntry.ApplyRole`.
- **Order change:** `InitCapacityModule` must run before `new EntityContext`.
- `ViewModule.Init` (Find `orientation`) must run before the first `Step`.
- Dependency direction unchanged. No new assembly.
- Replica: `SetReplicaMode(true)` still disables `CharacterController`; view restore does not call `Look`.

## Expected Behavior

- Forward/strafe input is mapped relative to `orientation`; `mesh` turns toward that planar target at `meshTurnSpeed`.
- Aim deltas rotate `orientation` only, within pitch limits. Root rotation stays identity.
- Planar displacement follows current mesh facing, so the character walks through the turn instead of sliding toward look while the model lags.
- Authority snapshots include mesh body yaw in `rotation` and view pose; replicas interpolate both; prediction view mismatch uses the existing restore/replay path.
- Replica interpolation snap fires on large root **or** view rotation jumps.

## Alternatives Rejected

- Keep two root modules: still fragments one transform and leaves view without an owner.
- Put `ViewModule` on the `orientation` GameObject: rejected by the user; Find from root is enough.
- Reconstruct view only from aim on replicas (no protocol field): rejected by the user.
- Aim remains a world facing vector: rejected; cannot drive pitch or independent look.
- Local-only view (no Step/rollback): breaks prediction and replica look.
- Wire yaw/pitch floats instead of `Quat`: rejected; reuse existing `Quat` helpers. Euler wrap stays inside `ViewModule` / `ViewRollbackState`.

## Risks and Open Questions

- Duplicate locomotion folders (`StateMachine/` vs `LocomotionStateMachine/`) may both reference `MovementModule`. Apply must compile; do not merge trees unless one is unused.
- `EntitySimulation` working copies have drifted naming versus `EntityContext`. Apply uses `EntityContext` as the data-holder source of truth.
- Mouse delta `* dt` may feel tick-rate dependent; tune `aimHorizontalSpeed` / `aimVerticalSpeed` after playtest.
- `Transform.Find("orientation")` does not search nested children. The current prefab uses a direct child.
- No remaining product questions that block this design.

## Verification

- Unity compile after proto regeneration; Console has no missing-script errors on `NetPlayer`.
- LocalPlay: look pitches/yaws on `orientation`; WASD moves relative to look; body turns only while moving.
- Authority + Replica: replica `orientation` follows snapshot view with interpolation; large view snaps teleport look.
- Predict: forced view mismatch restores and replays without leaving `orientation` at identity.
- `git diff --check` on edited files.
- Manual: proto field 11 present on `Character_Snapshot`; generated C# not hand-patched.
