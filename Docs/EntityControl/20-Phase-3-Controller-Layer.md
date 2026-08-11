# Phase 3 Controller Layer

Phase 3 goal: turn controllers into control-source orchestrators instead of behavior containers.

## Added

Location:

`Assets/Scripts/GamePlay/EntityControl/Controllers`

Types:

- `EntityDriveMode`
- `EntityControllerBase`
- `PlayerController`
- `ReplicaController`
- `AIController`

## Controller Binding

`EntityControllerBase` implements `IEntityController` and centralizes:

- `Target`
- `HasTarget`
- `DriveMode`
- `Bind(IEntityControlTarget target)`
- `Unbind()`
- `OnBound`
- `OnUnbound`

Reusable old controller implementations were moved into `EntityControl/Controllers` and renamed to the official controller types:

- `PlayerController`
- `AuthorityController`
- `ReplicaController`

## Drive Modes

`EntityDriveMode` defines the current control-source modes:

- `None`
- `LocalInput`
- `Prediction`
- `Authority`
- `Replica`
- `AI`

Mapping:

| Controller | Drive mode |
| --- | --- |
| `PlayerController` | `LocalInput` when local play, otherwise `Prediction` |
| `AuthorityController` | `Authority` |
| `ReplicaController` | `Replica` |
| `AIController` | `AI` |

## Input Intent Routing

`NetDriverInput.ApplyTo` now targets `IEntityIntentReceiver` instead of concrete `EntityCharacter`.

This keeps the edge-detection logic in one compatibility utility while allowing controllers to route intent through entity-layer contracts.

## Replacement Paths

The old controller names are no longer kept as inheritance wrappers:

- `LocalController` was replaced by `PlayerController`.
- `RemoteController` was replaced by `ReplicaController`.
- `AuthorityController` was moved under `EntityControl/Controllers`.
- `NetDriverInput` was moved under `EntityControl/Controllers`.

Reason: prediction, replay, authority simulation, and interpolation are still behavior-heavy, but reusable code should live directly in the official controller files instead of being wrapped through an old-name inheritance layer.

## Deferred

- Move prediction/replay logic out of `PlayerController`.
- Move authoritative input queue simulation out of `AuthorityController`.
- Move interpolation behavior out of `ReplicaController`.
- Replace `NetPositionSync` dependencies with sync module contracts.
- Re-add prefab controller components after the new entity/controller/sync assembly path is ready.
