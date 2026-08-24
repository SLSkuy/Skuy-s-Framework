# Transform and View Modules Tasks

## Preconditions
- [x] P1. Keep the 2026-08-24 confirmed boundaries: aim is a per-tick delta; movement follows view yaw; `ViewModule` is on the entity root and `Find("orientation")` during `Init`; `Character_Snapshot.view_rotation` is added. Do not implement camera follow or put `ViewModule` on the child GameObject.
- [x] P2. Proto generation uses the project `protoc` (`Protocol/protoc.exe`, same as `Tools/Protobuf/Proto Compiler`). Do not hand-edit `Assets/Scripts/GamePlay/Protocol/Generated/` except via that compiler.
- [x] P3. `Assets/Resources/NetPlayer.prefab` still has a direct child named `orientation`. If renamed, stop and update this plan.

## Implementation
- [x] I1. EntitySystem / Protocol: in `Assets/Scripts/GamePlay/Protocol/net_sync.proto`, add `Quat view_rotation = 11` to `Character_Snapshot`. Regenerate `Assets/Scripts/GamePlay/Protocol/Generated/NetSync.cs` with `Protocol/protoc.exe`.
- [x] I2. EntitySystem: add `viewRotation` to `Snapshot/State/EntitySimulationState.cs`. Add `Snapshot/State/ViewRollbackState.cs` (yaw, pitch, view angular velocity). Attach it on `EntityRollbackState`.
- [x] I3. Utils: extend `ProtoUtils.ToCharacterSnapshotMessage` and `ToSimulationState` to copy `viewRotation` <-> `view_rotation`.
- [x] I4. EntitySystem: in `Modules/ModuleType.cs`, replace `Position` and `Rotation` with `Transform` and `View`.
- [x] I5. EntitySystem: add `Modules/View/ViewModule.cs` on the entity root. `Init` binds `_orientation = transform.Find("orientation")` and logs an error if missing. `Look` applies aim deltas with `aimHorizontalSpeed` / `aimVerticalSpeed` and clamps pitch. Capture/restore use `ViewRollbackState` and `viewRotation`. Replica apply calls `Restore`, not `Look`.
- [x] I6. EntitySystem: add `Modules/Transform/TransformModule.cs` by merging `MovementModule` and body-yaw behavior from `RotationModule`. Root rotation stays identity. `Rotate` turns child `mesh` toward the orientation-mapped move vector at `meshTurnSpeed`. `Move` translates along current mesh planar forward. Keep `SetReplicaMode`, teleport, gravity, jump, and unused dash fields.
- [x] I7. EntitySystem: change `EntityCharacter.Init` to `InitConfig` -> `InitCapacityModule` -> `InitSimulationContext` -> `RegisterStates`. `GetOrAddComponent` `TransformModule` and `ViewModule` on the root; `Bind`/`Init` both; pass them into `EntityContext`.
- [x] I8. EntitySystem: replace `EntityContext.Movement` / `Rotation` with `Transform` / `View`. `EntitySimulation.Step` order: `View.Look` then `Transform.Rotate` then `View.ApplyWorldPose` then state machine. Capture/restore include view rollback and `viewRotation`.
- [x] I9. EntitySystem: retarget locomotion bases (`LocomotionStateMachine/` and any compiling `StateMachine/` copy) from `Movement.Move` / `Jump` to `Transform.Move` / `Jump`.
- [x] I10. EntitySystem: delete `Modules/Position/MovementModule.cs`, `Modules/Transform/RotationModule.cs`, and the empty `Modules/Position/` folder after all call sites compile.
- [x] I11. MultiPlaySystem: `CharacterPresentationAdapter.ApplyState` restores `TransformModule` then `ViewModule`. Fallback without modules still sets root pose and, if present, `orientation`.
- [x] I12. MultiPlaySystem: `CharacterReplicationEntry.ApplyRole` calls `TransformModule.SetReplicaMode` instead of `MovementModule.SetReplicaMode`.
- [x] I13. MultiPlaySystem: `CharacterSnapshotInterpolator` slerps `viewRotation`. Snap when root **or** view rotation exceeds `SyncConfig.rotationSnapThresholdDegrees`.
- [x] I14. MultiPlaySystem: `CharacterReplicationSystem` predict reconcile includes view angle against the existing rotation reconcile/snap thresholds; replay still goes through `EntityCharacter.Step`.

## Verification
- [x] V1. Unity compiles after proto regeneration. Console has no missing-script errors on `NetPlayer`.
- [ ] V2. LocalPlay: `orientation` yaws/pitches from aim; WASD moves relative to view yaw; body yaw turns only while moving; pitch respects `minAimPitch` / `maxAimPitch`.
- [ ] V3. Authority + Replica: replica `orientation` follows interpolated `view_rotation`; a large view jump snaps.
- [ ] V4. Predict: a forced view mismatch restores `ViewRollbackState` and replays without leaving `orientation` at identity.
- [ ] V5. `git diff --check` is clean for edited files. Generated proto is compiler output, not a hand patch.

## Documentation
- [x] D1. Update `Docs/Architecture/entity.md` module list and init order: `TransformModule` + `ViewModule`; `Find("orientation")`.
- [x] D2. Update `Docs/Architecture/simulation.md`: Step order (Look -> Rotate -> Move); Replica still does not `Step`; `SetReplicaMode` stays on `TransformModule`.
- [x] D3. Update `Docs/Architecture/networking.md`: `Character_Snapshot.view_rotation`; interpolator and reconcile include view pose.
- [x] D4. Set this design to `Approved` / `In Progress` when apply starts, and keep `Docs/Plan/README.md` aligned.
