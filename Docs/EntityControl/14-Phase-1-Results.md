# Phase 1 Results

Phase 1 goal: build the foundation only and avoid gameplay behavior rewrites.

## Completed

- Inspected direct coupling paths across entity, controller, module, FSM, and sync code.
- Confirmed target runtime layout under `Assets/Scripts/GamePlay/EntityControl`.
- Split the sync folder into `Core`, `Config`, `Snapshot`, and `Timing`.
- Moved reusable sync base types while preserving namespaces and `.meta` GUIDs.
- Kept `NetSyncUtils` in `Assets/Scripts/Utils` because stateless global helpers remain outside the EntityControl runtime layout.
- Added minimal boundary contracts for entity intent, simulation, state view, controller binding, modules, and sync snapshot capture/apply.

## Runtime Behavior

No existing behavior classes were migrated or rewritten in Phase 1.

Unchanged compatibility classes include:

- `EntityCharacter`
- `EntityContext`
- `EntityMotor`
- `EntityAnimator`
- `LocalController`
- `AuthorityController`
- `RemoteController`
- `NetDriverInput`
- `NetPositionSync`
- `NetEntityRoleAssembler`

## Validation Performed

Static checks completed:

- Confirmed moved reusable files no longer exist at old locations.
- Confirmed reusable sync files exist under the new `EntityControl/Sync` subfolders.
- Confirmed `NetSyncUtils` exists under `Assets/Scripts/Utils`.
- Confirmed new boundary contract type names are unique in `Assets/Scripts`.
- Confirmed no old runtime class was modified to implement the new contracts yet.

## Validation Blocked

Unity compile validation is still required.

The current command environment cannot run compile validation because these executables are not available from PATH or common install locations:

- `Unity`
- `dotnet`
- `msbuild`
- `csc`

Required manual validation:

- Open the project in Unity Editor.
- Let Unity import and recompile scripts.
- Check Console for compile errors.

## Phase 1 Exit Criteria

Phase 1 is complete from the file-organization and contract-definition side.

Before starting Phase 2, Unity Editor compile should be confirmed so any path/meta/compiler issues are caught while the change surface is still small.
