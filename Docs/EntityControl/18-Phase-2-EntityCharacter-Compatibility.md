# Phase 2 EntityCharacter Compatibility

Task 2.4 keeps `EntityCharacter` as the current compatibility path.

## Decision

Keep `EntityCharacter` as the runtime component used by existing prefabs and controller/sync code.

Do not migrate prefabs to `PlayerEntity` or other derived entity scripts in this step.

## Prefab Reference Check

`Assets/Resources/NetPlayer.prefab` in the repository baseline references:

- `EntityAnimator` script GUID: `474b691ce61d4df1a9fbe4a43d66f819`
- `EntityCharacter` script GUID: `46837910ba0645d8906d6363aaae08b2`

The baseline `EntityCharacter` component serializes:

- `tickDrive`

## Current Worktree Note

The current working tree has an existing uncommitted `Assets/Resources/NetPlayer.prefab` change that removes these MonoBehaviour components from the prefab:

- `EntityAnimator`
- `EntityCharacter`
- `NetEntityIdentity`
- `NetEntityRoleAssembler`
- `NetTransformSync`

The project owner confirmed this deletion is intentional. The components will be re-added after the refactor reaches the new entity/controller/sync assembly path.

## Code Dependency Check

The following compatibility paths still depend on concrete `EntityCharacter`:

- `LocalController`
- `AuthorityController`
- `RemoteController`
- `NetDriverInput`
- `NetPositionSync`
- `NetEntityRoleAssembler`

These references should be migrated in later controller/sync phases after `IEntityControlTarget` adoption.

## Why No Prefab Migration Yet

Derived entity scripts now exist, but they inherit from `EntityCharacter` only as role entry shells.

Replacing prefab components now would create unnecessary Unity component migration risk while the rest of the codebase still requests `EntityCharacter` through `RequireComponent`, `GetComponent<EntityCharacter>()`, and concrete method parameters.

## Deferred

- Replace controller fields and `GetComponent<EntityCharacter>()` calls with `BaseEntity` or `IEntityControlTarget`.
- Decide which prefabs should use `PlayerEntity`, `RemotePlayerEntity`, or other derived entity scripts.
- Perform prefab migration only after controller and sync compatibility are ready.
