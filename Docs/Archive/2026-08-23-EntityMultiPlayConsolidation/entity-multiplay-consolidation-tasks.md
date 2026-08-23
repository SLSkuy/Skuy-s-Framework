# Entity and MultiPlay Consolidation Tasks

Ordered checklist derived from `entity-multiplay-consolidation-design.md`. Do not mark implementation items complete while planning.

## Preconditions

- [x] P1. Explicitly confirm this plan before implementation. The `AGENTS.md` gate applies: public types and call chains change in `EntitySystem` and `MultiPlaySystem`. Confirmation must cover Option A, LocalPlay as the offline path, no non-character `EntityObject`, keeping `AIController` as a stub, keeping dash/focus fields, deleting the types listed in the design, and the MultiPlay folder split. Record Plan Status `Approved` and Confirmation Gate `Confirmed` in the design, this task list, and `Docs/Plan/README.md`. Confirmed by user “确认 /apply” on 2026-08-23; design gate set to Confirmed and status In Progress.
- [x] P2. Capture current prefab/component usage for `EntityCharacter`, `NetworkObjectIdentity`, `PlayerController`, `AIController`, and `CharacterPresentationAdapter` (including `Resources/NetPlayer`) so script moves keep `.meta` GUIDs. `Assets/Resources/NetPlayer.prefab` has `CharacterPresentationAdapter` (`f4c02f2de8ef2c54ca5c58bfb65184af`), `EntityCharacter` (`46837910ba0645d8906d6363aaae08b2`), `PlayerController` (`eb4a1afe376a480483763f670fc99f28`), `NetworkObjectIdentity` (`4da1e754ec294f628cdf67d55fe4f1c1`). `AIController` is not on this prefab. Script `.meta` GUIDs must move with files.



## Implementation

- [x] I1. `EntitySystem`: collapse `EntityObject<TConfig, TContext>`, `EntityObjectContext<TConfig>`, and `EntitySimulationObject` into `EntityCharacter`. `EntityContext` becomes a concrete context. `EntityCharacter` owns `EntitySimulation` and exposes `Init`, `Step`, `CaptureSimulationState`, `CaptureRollbackState`, and `RestoreRollbackState`. Do not implement `IEntityIntentReceiver`.
- [x] I2. `EntitySystem`: retarget `IEntityModule`, `EntityModuleBase`, `EntityControllerBase`, `PlayerController`, `CharacterReplicationEntry`, and `CharacterPresentationAdapter` from `EntitySimulationObject` to `EntityCharacter`. `PlayerController.Init` takes only `EntityCharacter`.
- [x] I3. `EntitySystem`: delete unused types and methods: `EntityObjectConfig`, `EntityControllerUtils`, `EntityDriveMode`, `IEntityController`, `IEntityIntentReceiver`, `IEntityStateView`, `EntityCommandQueue.TryDequeue`, `EntityInputCommandBuilder.RestoreBaseline`, and the `EntityCharacter` intent methods. Keep `IEntitySimulation` / `IEntityStateStore` only on `EntitySimulation` if still useful internally; do not re-declare them on the MonoBehaviour.
- [x] I4. `EntitySystem`: keep `AIController` as an empty `EntityControllerBase` stub. Keep dash fields on `MovementRollbackState` / `MovementModule` and `IsFocus` on `EntityContext` / `EntityRollbackState`. Do not add dash, focus, or AI command behavior.
- [x] I5. `MultiPlaySystem`: make `EntitySimulationMode.LocalPlay` the official offline path. Scene/runtime identities with role `LocalPlay` register without `NetClient`/`NetServer`/`MultiPlayManager`. `AdvanceTick` must step LocalPlay when both net systems are null. Keep networked prefabs from registering as LocalPlay before `Init` (id `0` + non-LocalPlay still skipped on `OnEnable`). Remove `[Obsolete]` from the live `Register` API or add `RegisterLocalPlay` as a named wrapper without a second tick loop.
- [x] I6. `MultiPlaySystem`: move remaining `Runtime/` files (with `.meta`) into `Time/`, `Identity/`, `Replication/`, `Input/`, `Prediction/`, and `Presentation/` as specified in the design. Do not invent extra wrapper types. Extract shared test prefab spawn from `ClientSimulator` and `ServerSimulator` into one helper under `TestSimulator/`. `TestPlayerSpawner` is in `TestSimulator/`; Interpolation snapshot types are in `Interpolation/`; `Runtime/` has been removed after Unity `MoveAsset` (script GUIDs preserved).
- [x] I8. Move `IEntitySnapshot` and `SnapshotBuffer<T>` from `EntitySystem/Snapshots/` into MultiPlay `Interpolation/` (keep script `.meta` GUIDs). Move `CharacterSnapshot` and `CharacterSnapshotInterpolator` into the same folder. Delete empty EntitySystem `Snapshots/`. Namespace `GamePlay.MultiPlaySystem`. `EntityPredictionHistory` stays in EntitySystem.
- [x] I7. `MultiPlaySystem`: `CharacterReplicationEntry.ApplyRole` uses `EntityCharacter`, enables `PlayerController` for LocalPlay and Predict, enables `AIController` only for `Authority && OwnerClientId == 0`, and owns Replica `CharacterController` enable/disable in one place (`ApplyRole` or `MovementModule.SetReplicaMode`, not both). `CharacterPresentationAdapter` must not override that choice.



## Verification

- [x] V1. Gameplay scripts compile with zero remaining references to deleted types (`EntitySimulationObject`, `EntityDriveMode`, `IEntityIntentReceiver`, `EntityObjectConfig`, `EntityControllerUtils`). Unity Console errors empty after snapshot move (2026-08-23); Assets grep has no remaining references.
- [x] V2. Offline LocalPlay: Play Mode or editor play with `NetClient`/`NetServer` not started; a scene `EntityCharacter` with `NetworkObjectIdentity` role `LocalPlay` and `PlayerController` moves on `NetworkTimeSystem` ticks. User reported Play Mode pass on 2026-08-23, including after the Runtime folder move.
- [x] V3. Online test path: `SyncTestPanel` server + client still spawn Authority/Predict/Replica; Predict still sends `NetSync.Player_Input` when a fast channel exists; Replica interpolates and does not call `EntitySimulation.Step`. User reported Play Mode pass on 2026-08-23, including after the Runtime folder move.
- [x] V4. Replica collision: interpolated pose updates and the driving `CharacterController` is disabled. Rollback structs still contain dash and `IsFocus` fields. User reported Play Mode pass on 2026-08-23, including after the Runtime folder move.
- [x] V5. `git diff --check` is clean for implementation files. Unity Console has no missing-script errors on `NetPlayer` or scene characters after the folder move. User reported review pass on 2026-08-23 after the Runtime split: LocalPlay, online test, and Replica paths all behave normally; no missing scripts.



## Documentation

- [x] D1. Update `Docs/Architecture/entity.md` to the flattened `EntityCharacter` shape, removed types, kept AI stub, and kept dash/focus fields.
- [x] D2. Update `Docs/Architecture/simulation.md` so LocalPlay is documented as the no-server shared `EntitySimulation.Step` path driven by `NetworkTimeSystem`.
- [x] D3. Update `Docs/Architecture/networking.md` for the new MultiPlay folders, identity ownership, and Replica collision rule.
- [x] D4. Update `.agents/rules/Architecture.md` namespace/examples that still say `GamePlay.NetSync` or place `NetworkObjectIdentity` in EntitySystem.
- [x] D5. After implementation, set Plan Status to `In Progress` or `Complete` to match actual work, and keep `Docs/Plan/README.md` aligned with the design. Status is Complete after user review pass on 2026-08-23.