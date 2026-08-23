# Entity and MultiPlay Consolidation

## Workflow Status

Plan Status: Archived
Confirmation Gate: Confirmed
Confirmation Evidence: On 2026-08-23 the user selected Option A and resolved the open questions (LocalPlay offline path, no non-character `EntityObject`, keep `AIController` stub, keep dash/focus fields). On 2026-08-23 the user sent “确认 /apply”, approving the public-API and folder-split list in this document.

## Background and Goal

`EntitySystem` currently stacks a generic object template, a simulation object, and `EntityCharacter`, plus unused controller/intent/config types. `MultiPlaySystem` keeps almost every runtime type in `Runtime/` and wraps Entity queues, prediction history, and snapshots in `Character*` helpers. Two role enums (`EntityDriveMode` and `EntitySimulationMode`) overlap, and Replica collision is applied twice.

The goal is to flatten the character object to a single scene type, delete proven-dead APIs, classify MultiPlay by duty, keep the two-module dependency direction, and make `EntitySimulationMode.LocalPlay` a first-class offline path so a client can tick and respond without starting a server or connecting a client.

Success is observable when:

- Scene-placed characters with `NetworkObjectIdentity` role `LocalPlay` move from local input with `NetServer`/`NetClient` stopped.
- Authority, Predict, and LocalPlay still share `EntitySimulation.Step`.
- Replica still interpolates and never calls `Step`.
- `AIController` still exists as a disabled stub on Authority entities with `OwnerClientId == 0`.
- Dash and focus fields remain on rollback/context even though no dash/focus gameplay is added.

## Scope

In scope:

- Flatten `EntityObject<TConfig, TContext>` / `EntityObjectContext<TConfig>` out of the character path.
- Delete unused Entity types and obsolete methods listed below.
- Remove `EntityDriveMode` and `IEntityIntentReceiver`.
- Keep `AIController` as an empty bindable stub; keep enabling it only as a future hook.
- Keep dash/focus data fields; do not add dash or focus gameplay.
- Re-folder `MultiPlaySystem/Runtime/` by duty; collapse wrappers that add no policy.
- Make LocalPlay registration and ticking independent of `MultiPlayManager`, `NetClient`, and `NetServer`.
- Fix Replica `CharacterController` ownership so only one place enables or disables it.
- Update `Docs/Architecture/entity.md`, `simulation.md`, `networking.md`, and the stale NetSync names in `.agents/rules/Architecture.md`.

Out of scope:

- Merging `EntitySystem` and `MultiPlaySystem` into one module.
- New `.asmdef` files.
- Hand-edits to `Assets/Scripts/GamePlay/Protocol/Generated/` or protobuf layout.
- Implementing AI behavior, dash, focus targeting, combat, or animation modules.
- Changing KCP/TCP transport or `NetClient`/`NetServer` session lifecycle.
- Mixing an in-progress LocalPlay scene object with a later online session in the same Play Mode run (test `Stop()` already tears down simulator entities).

## Assumptions and Decisions

- Dependency direction stays `EntitySystem` <- `MultiPlaySystem` <- `Network`.
- Near-term simulated scene object is only `EntityCharacter`. There is no second `EntityObject` subclass to preserve the generic template for.
- `AIController` remains a placeholder: no command policy, still present on prefabs, still toggled by `CharacterReplicationEntry` (`Authority && OwnerClientId == 0`).
- `MovementRollbackState` dash fields, `EntityContext.IsFocus`, and `EntityCommandButtons` attack/interact flags stay. No start-dash or focus API is added.
- `EntityCommandQueue` and `EntityPredictionHistory` stay in `EntitySystem` as protocol-free primitives. `IEntitySnapshot` and `SnapshotBuffer<T>` live in MultiPlay `Interpolation/` because EntitySystem has no remaining consumers.
- Offline play uses existing `EntitySimulationMode.LocalPlay`, not a fifth role and not a new subsystem.
- `GameCore` already registers `NetworkTimeSystem` and `CharacterReplicationSystem`; that pair is the offline host. `MultiPlayManager` stays a test fixture.
- Confirmation gate applies: public types and call chains change in both gameplay runtime modules.

## Repository Evidence

Current character inheritance (`Assets/Scripts/GamePlay/EntitySystem/`):

```text
EntityObject<TConfig, TContext>          # only EntitySimulationObject derives
  -> EntitySimulationObject              # IEntitySimulation + IEntityStateStore + IEntityStateView
        -> EntityCharacter               # only concrete entity; also IEntityIntentReceiver
              owns EntitySimulation      # real Step / capture / restore
```

`EntityContext` is the only `EntityObjectContext<EntityConfig>`. `EntityConfig` does not inherit `EntityObjectConfig`.

Dead or unread types (search has no consumers outside their declaring files):

- `EntityObjectConfig`
- `EntityControllerUtils`
- `EntityCommandQueue.TryDequeue` (`[Obsolete]`)
- `EntityInputCommandBuilder.RestoreBaseline`
- `IEntityIntentReceiver` and `EntityCharacter` Move/Aim/Jump/Sprint/Toggle methods (simulation writes `EntityContext` from `EntityInputCommand`)
- `IEntitySimulation` / `IEntityStateStore` / `IEntityStateView` as consumed types (`CharacterReplicationSystem` calls `EntitySimulationObject` methods)
- `IEntityController` as a consumed type
- `EntityDriveMode` (only returned from controllers, never branched on)
- `EntitySimulationObject.HasIdentity`
- `ModuleType.Animation` (no module)
- Dash execution API (fields exist; nothing sets `_isDashing` true except restore)

Role duplication:

- `EntityDriveMode` in EntitySystem versus `EntitySimulationMode` in MultiPlaySystem. Replication already keys off `EntitySimulationMode` (`CharacterReplicationSystem.AdvanceTick`, `CharacterReplicationEntry.ApplyRole`).

LocalPlay already exists and is the offline path to keep:

- `EntitySimulationMode.LocalPlay`
- `NetworkObjectIdentity` defaults to `LocalPlay` and id `0`
- `RegisterReplication` allows id `0` only for LocalPlay
- `OnEnable` skips id `0` so networked prefabs do not register as LocalPlay before `Init`
- `Start` calls `RegisterReplication`, which is how scene LocalPlay objects join
- `CharacterReplicationSystem.AdvanceTick` runs `SimulateLocalPlayTick` without requiring `_client` or `_server`
- `SimulateLocalPlayTick` samples `PlayerController` and calls `EntitySimulationObject.Step`

Gaps: LocalPlay is under-documented; `PlayerController.Init` still takes a `localPlay` flag only to feed unused `EntityDriveMode`; `CharacterReplicationSystem.Register` is `[Obsolete]` while still the real entry; `AdvanceTick` always tries `BindNetworkHandlers`.

MultiPlay duplication:

- `CharacterInputBuffer` wraps `EntityCommandQueue<InputState>` + `EntityInputCommandBuilder`
- `CharacterPredictionController` wraps `EntityPredictionHistory` plus tick-anchor policy
- `CharacterSnapshot` is tick + `EntitySimulationState`
- `MovementModule.SetReplicaMode` and `CharacterPresentationAdapter.ApplyRole` both touch `CharacterController`
- `ClientSimulator` and `ServerSimulator` both load `Resources/NetPlayer` and call identity `Init`

## Data Sources and Ownership

| Data | Owner | Consumers |
| --- | --- | --- |
| `InputState` from `IInputStateProvider` | Framework input / `GameCore.LocalInput` | `PlayerController.SampleInput`, `EntityInputCommandBuilder` |
| `EntityInputCommand` | EntitySystem | `EntitySimulation.Step` |
| `EntitySimulationState` / `EntityRollbackState` | EntitySystem | prediction replay, snapshots, presentation |
| `EntityConfig` | EntitySystem ScriptableObject | movement, rotation, locomotion |
| `SyncConfig` | MultiPlaySystem ScriptableObject | tick rate, buffers, reconcile thresholds |
| `NetSync.Player_Input` / `NetSync.World_Snapshot` | Generated protocol | MultiPlay only |
| Tick | `NetworkTimeSystem` + `NetworkTickSystem` | all four simulation modes |

EntitySystem must not read `SyncConfig` or NetSync messages. MultiPlay must not own locomotion or state-machine rules.

## Selected Design

### 1. Entity object shape

Replace the generic three-type stack with one scene component:

```text
EntityCharacter : MonoBehaviour
  Init()
  Step / CaptureSimulationState / CaptureRollbackState / RestoreRollbackState
  EntityContext Context
  EntitySimulation (private)
  IEntityObjectIdentity (optional component)
```

`EntityContext` becomes a concrete type (config + state machine + movement/rotation + input flags). `EntitySimulation` remains the only stepper implementation.

Call sites that currently `GetComponent<EntitySimulationObject>()` use `EntityCharacter`. Prefabs already serialize `EntityCharacter`, so Unity script GUIDs on the character component stay valid.

Keep `IEntityObjectIdentity` on the MultiPlay identity component so Entity still does not reference MultiPlay.

### 2. Controllers and enums

- Delete `EntityDriveMode`.
- Keep `EntityControllerBase` Bind/Unbind against `EntityCharacter`.
- `PlayerController.Init(EntityCharacter entity)` only binds and resolves `IInputStateProvider`. LocalPlay and Predict both sample input; role lives on `NetworkObjectIdentity`.
- Keep `AIController` as an empty subclass. `CharacterReplicationEntry` still sets `enabled` when `Authority && OwnerClientId == 0`. No AI commands in this plan.
- Delete `IEntityIntentReceiver`. Input reaches simulation only as `EntityInputCommand`.

Single role enum, owned by MultiPlay:

```text
EntitySimulationMode { Authority, Predict, Replica, LocalPlay }
```

### 3. LocalPlay (offline client simulation)

LocalPlay is the no-server path. It is not Predict-without-snapshots.

```text
GameCore
  registers NetworkTimeSystem + CharacterReplicationSystem
        |
        v
NetworkTimeSystem.Tick  (local clock, no NetClient required)
        |
        v
CharacterReplicationSystem.AdvanceTick
  Identity.IsLocalPlay -> sample PlayerController -> EntityCharacter.Step
  no Player_Input send, no World_Snapshot consume
```

Rules:

- Scene or runtime objects with `NetworkObjectIdentity.Role == LocalPlay` register even when `NetworkObjectId == 0`, using the instance id as the dictionary key (existing `GetRegistrationId` behavior).
- `OnEnable` must still refuse id `0` unless the serialized role is already LocalPlay, so networked prefabs cannot register as LocalPlay before `Init`.
- `AdvanceTick` must run LocalPlay when `_client` and `_server` are null. `BindNetworkHandlers` may stay lazy and no-op.
- `PlayerController.enabled == true` for LocalPlay and Predict.
- Presentation uses the simulated transform directly; no interpolation, no reconcile.
- Starting `MultiPlayManager` client/server stays a separate test path: those objects call `Init(id, Predict|Replica|Authority, owner)` with a non-zero id.

Optional small helper (same module, not a new subsystem): `CharacterReplicationSystem.RegisterLocalPlay(NetworkObjectIdentity)` as a clearly named wrapper around `Register`, used by scene objects and any offline spawner. Do not add a second tick loop.

### 4. MultiPlay classification

Keep types; move files (and `.meta`) out of a flat `Runtime/`:

```text
MultiPlaySystem/
  Config/             SyncConfig (already here)
  Time/               NetworkTimeSystem, NetworkTickSystem
  Identity/           NetworkObjectIdentity, EntitySimulationMode
  Replication/        CharacterReplicationSystem, CharacterReplicationEntry
  Input/              CharacterInputBuffer, CharacterInputValidator
  Prediction/         CharacterPredictionController
  Interpolation/      IEntitySnapshot, SnapshotBuffer, CharacterSnapshotInterpolator, CharacterSnapshot
  Presentation/       CharacterPresentationAdapter
  TestSimulator/      existing test types
```

Wrapper policy:

- Keep `CharacterInputBuffer` (authority enqueue window + predicted command build).
- Keep `CharacterPredictionController` (input tick allocation, confirm, replay list).
- Keep `CharacterSnapshot` as the MultiPlay `IEntitySnapshot` adapter around `EntitySimulationState`.
- Do not add another queue or history type.
- Extract shared test prefab spawn from `ClientSimulator` / `ServerSimulator` into one test helper under `TestSimulator/`.
- Remove `[Obsolete]` from `CharacterReplicationSystem.Register` or replace it with `RegisterLocalPlay` plus `Register` without obsolete, since it is the live entry.

Replica collision: only `CharacterReplicationEntry.ApplyRole` (or only `MovementModule.SetReplicaMode`) enables/disables `CharacterController`. `CharacterPresentationAdapter.ApplyRole` must not fight that decision. Replica remains presentation-only.

### 5. Fields kept for later

Leave in place, unused by new gameplay in this plan:

- `MovementRollbackState` dash fields and `MovementModule` dash storage/`TickDash`
- `EntityContext.IsFocus` and rollback copy
- `EntityCommandButtons` PrimaryAttack, SpecialAttack, SpecialAction, Interact
- `ModuleType.Animation`

Delete comments that claim dash or intent APIs are active if they are not.

### 6. Control flow after change

```text
                    NetworkTimeSystem.Tick
                              |
            CharacterReplicationSystem.AdvanceTick
                              |
        +---------------------+---------------------+--------------------+
        |                     |                     |                    |
   LocalPlay             Predict               Authority            Replica
   sample input          sample+send           dequeue input        no Step
   Step                  Step+history          Step                 interpolate
   no net                reconcile on snap     broadcast snap       ApplyState
```

## Modules and Files Affected

### EntitySystem — modify

- `Entities/EntityCharacter.cs` — absorb init/step surface; drop intent API
- `Entities/EntityContext.cs` — stop deriving from generic object context; keep focus fields
- `Simulation/EntitySimulation.cs` — keep; may drop unused interface implementations if nothing consumes them
- `Controllers/Player/PlayerController.cs` — `Init(EntityCharacter)`
- `Controllers/AI/AIController.cs` — keep stub; drop `EntityDriveMode`
- `Controllers/EntityControllerBase.cs` — target `EntityCharacter`
- `Modules/EntityModuleBase.cs`, `IEntityModule.cs` — bind `EntityCharacter`
- `Modules/Position/MovementModule.cs` — keep dash fields; Replica mode only if it remains the single collision owner
- `Commands/EntityCommandQueue.cs` — delete `TryDequeue`
- `Input/EntityInputCommandBuilder.cs` — delete `RestoreBaseline`

### EntitySystem — delete

- `Entities/EntityObject/EntityObject.cs`
- `Entities/EntityObject/EntitySimulationObject.cs`
- `Entities/EntityObject/EntityObjectContext.cs`
- `Config/EntityObjectConfig.cs`
- `Controllers/EntityDriveMode.cs`
- `Controllers/IEntityController.cs`
- `Controllers/EntityControllerUtils.cs`
- `Contracts/IEntityIntentReceiver.cs`
- `Contracts/IEntityStateView.cs` (unread)
- `Contracts/IEntitySimulation.cs` and `Contracts/IEntityStateStore.cs` if after flatten no remaining implementer/consumer pair needs them; otherwise keep only on `EntitySimulation`

Provisional: keep `IEntityObjectIdentity.cs`.

### MultiPlaySystem — modify / move

- `Runtime/CharacterReplicationSystem.cs` -> `Replication/`
- `Runtime/CharacterReplicationEntry.cs` -> `Replication/` (component type `EntityCharacter`; LocalPlay controller init without drive mode)
- `Runtime/NetworkObjectIdentity.cs` -> `Identity/`
- `Runtime/EntitySimulationMode.cs` -> `Identity/`
- `Runtime/NetworkTimeSystem.cs`, `NetworkTickSystem.cs` -> `Time/`
- `Runtime/CharacterInputBuffer.cs`, `CharacterInputValidator.cs` -> `Input/`
- `Runtime/CharacterPredictionController.cs` -> `Prediction/`
- `Runtime/CharacterSnapshotInterpolator.cs`, `CharacterSnapshot.cs` plus EntitySystem `Snapshots/` (`IEntitySnapshot`, `SnapshotBuffer`) -> `Interpolation/` (script `.meta` GUIDs preserved)
- `Runtime/CharacterPresentationAdapter.cs` -> `Presentation/` (stop duplicate controller toggle)
- `TestSimulator/ClientSimulator.cs`, `ServerSimulator.cs` — shared spawn helper
- `TestSimulator/MultiPlayManager.cs` — unchanged duty; still not required for LocalPlay

### Other call sites

- `Assets/Scripts/Core/GameCore.cs` — keep registering time + replication; no MultiPlayManager for offline
- `Assets/Scripts/Utils/NetSyncUtils.cs` — no protocol change expected
- Prefabs that `GetComponent<EntitySimulationObject>()` in scripts are code-only; serialized `EntityCharacter` stays

### Documentation

- `Docs/Architecture/entity.md`
- `Docs/Architecture/simulation.md`
- `Docs/Architecture/networking.md`
- `.agents/rules/Architecture.md` (namespace table still says `GamePlay.NetSync` and puts `NetworkObjectIdentity` under EntitySystem)

No `.asmdef` adds or protocol file edits.

## Interface and Call-Chain Changes

Breaking (same assembly, mechanical updates):

- `EntitySimulationObject` removed; public bind/init/get-component surface becomes `EntityCharacter`.
- `PlayerController.Init(EntitySimulationObject, bool localPlay)` -> `Init(EntityCharacter entity)`.
- `IEntityModule.Bind(EntitySimulationObject)` -> `Bind(EntityCharacter)`.
- `IEntityController` / `EntityDriveMode` / `IEntityIntentReceiver` removed.
- `CharacterReplicationSystem.Register` remains the registration API without obsolete, plus optional `RegisterLocalPlay`.

Unchanged:

- `EntitySimulation.Step(uint, float, in EntityInputCommand)`
- `EntitySimulationMode` values including `LocalPlay`
- NetSync message shapes and `NetSync` namespace
- `NetworkTimeSystem.Tick` signature
- Subsystem lifecycle `_Init` / `Init` / `Destroy` (no `Ensure*` / lazy self-heal)
- No `.asmdef` dependency graph (project has none)

Lifecycle: `NetworkObjectIdentity` still registers from `Start`/`Init` and unregisters from `OnDisable`. LocalPlay still uses instance id when network id is 0. Character `Init()` still runs from `CharacterReplicationEntry.ApplyRole`, not from `Awake` self-heal.

Compatibility: existing `EntityCharacter` + `NetworkObjectIdentity` prefabs remain valid if component types on the prefab are already `EntityCharacter`. Inspector fields on `NetworkObjectIdentity` (`networkObjectId`, `ownerClientId`, `role`) stay.

## Lifecycle and Dependency Impact

- No new subsystems. Offline play reuses `NetworkTimeSystem` and `CharacterReplicationSystem` registered by `GameCore`.
- `MultiPlayManager` stays test-only and must not become a requirement for LocalPlay.
- EntitySystem still must not reference `GamePlay.MultiPlaySystem` or `Network`.
- Tick order unchanged: `NetworkTimeSystem` (priority 40) then replication (NetSyncManager 100).

## Expected Behavior

- Offline: a scene character with role LocalPlay, `PlayerController`, and no net session steps every network tick from sampled input and the transform responds the same frame pipeline as today (fixed tick, not `Update` locomotion).
- Online owner: Predict still sends `Player_Input` and reconciles on `World_Snapshot`.
- Online other: Replica interpolates; `CharacterController` is not the driving collider.
- Authority: queued inputs step simulation and emit snapshots.
- `AIController` does nothing but can be enabled on unowned authority characters.
- Dash/focus fields round-trip through rollback capture/restore without new input.

## Alternatives Rejected

- Merge EntitySystem and MultiPlaySystem: reverses documented dependency direction and pulls NetSync into simulation.
- Move `EntityPredictionHistory` into MultiPlay in this pass: extra move cost; prediction history is still consumed by Entity-side rewind storage. Snapshot types moved after verification because EntitySystem had no remaining readers.
- Replace LocalPlay with Predict-when-disconnected: Predict without snapshots would accumulate unconfirmed history and still try to send input. LocalPlay is the honest offline mode.
- Delete `AIController`: user asked to keep the stub.
- Delete dash/focus fields: user asked to keep them for later.

No external library is adopted. Unity Netcode `NetworkObject` split is used only as a reminder that identity stays in MultiPlay.

## Risks and Open Questions

Risks:

- Flattening `EntitySimulationObject` requires updating every bind/get-component site or compilation fails.
- Moving MultiPlay scripts must move `.meta` GUIDs or prefab references break.
- LocalPlay `OnEnable`/`Start` order must not reintroduce early LocalPlay registration for networked prefabs with id 0.
- Replica collision regression if both presentation and movement still toggle the controller.

Open questions (defaults if confirmation does not override):

- Keep `IEntitySimulation` only on `EntitySimulation` (default: keep the stepper interfaces on `EntitySimulation`, not on the MonoBehaviour).
- Shared test spawn helper name (default: `TestPlayerSpawner` under `TestSimulator/`).

## Verification

- Compile the gameplay scripts; no remaining references to deleted types.
- Manual or Play Mode: scene LocalPlay character with `NetClient`/`NetServer` not started; input moves the character on `NetworkTimeSystem` ticks.
- Manual: `SyncTestPanel` server + client still spawns Authority/Predict/Replica and remote interpolation still runs.
- Replica: interpolated pose updates; driving `CharacterController` disabled.
- Predict: input message still sent when a fast channel exists.
- Rollback capture still copies dash and `IsFocus` fields (inspect struct or debug), without requiring dash gameplay.
- `AIController` still present and remains disabled for Predict/LocalPlay/Replica.
- Architecture docs match the new folders and type list.
- `git diff --check` on the plan files and later on implementation.
