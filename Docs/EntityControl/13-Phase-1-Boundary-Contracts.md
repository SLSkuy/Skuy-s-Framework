# Phase 1 Boundary Contracts

This document records the minimal contracts added in task 1.4.

Rule: contracts are introduced as extension points only. Existing runtime behavior is not migrated in this step.

## Entity Core Contracts

Location:

`Assets/Scripts/GamePlay/EntityControl/Core`

| Contract | Purpose |
| --- | --- |
| `IEntityIntentReceiver` | Receives control intent such as move, aim, jump, sprint, and run toggle. |
| `IEntitySimulation` | Exposes external simulation drive through `TickDrive` and `Simulate(float)`. |
| `IEntityStateView` | Read-only state view for animation, sync, debug, and future modules. |
| `IEntityControlTarget` | Combined target interface for controllers. It composes intent, simulation, and state view contracts. |

## Controller Contract

Location:

`Assets/Scripts/GamePlay/EntityControl/Controllers`

| Contract | Purpose |
| --- | --- |
| `IEntityController` | Defines binding between a control source and an `IEntityControlTarget`. |

## Module Contract

Location:

`Assets/Scripts/GamePlay/EntityControl/Modules`

| Contract | Purpose |
| --- | --- |
| `IEntityModule` | Defines entity module binding and enable/disable behavior. |

## Sync Entry Contracts

Location:

`Assets/Scripts/GamePlay/EntityControl/Sync/Core`

| Contract | Purpose |
| --- | --- |
| `INetSyncSnapSource<TSnapshot>` | Captures snapshots from an entity or module. |
| `INetSyncSnapshotReceiver<TSnapshot>` | Applies snapshots to an entity or module. |

Existing `INetSyncComponent` remains the role-configuration contract for current sync components.

## Deferred Implementation

The following classes do not implement the new contracts yet:

- `EntityCharacter`
- `LocalController`
- `AuthorityController`
- `RemoteController`
- `NetPositionSync`
- `EntityMotor`
- `EntityAnimator`

Reason: Phase 1 reserves boundaries first. Implementing these contracts will happen in reviewed follow-up steps after compatibility impact is checked.
