# Phase 1 Reusable Type Migration

This document records the reusable type moves performed in task 1.3.

## Rule Applied

Moved files keep their original namespace and `.meta` GUID.

Reason: Phase 1 only organizes stable reusable types. Namespace churn is deferred to a later reviewed cleanup step.

## Moved Under `Assets/Scripts/GamePlay/EntityControl/Sync`

| Type | Old location | Namespace |
| --- | --- | --- |
| Type | New location | Old location | Namespace |
| --- | --- | --- | --- |
| `NetEntityRole` | `Sync/Core/NetEntityRole.cs` | `Assets/Scripts/GamePlay/EntitySystem/NetEntityRole.cs` | `GamePlay.EntitySystem` |
| `SyncModuleID` | `Sync/Core/SyncModuleID.cs` | `Assets/Scripts/GamePlay/MultiPlaySystem/Component/SyncModuleID.cs` | `GamePlay.EntitySystem` |
| `INetSyncComponent` | `Sync/Core/INetSyncComponent.cs` | `Assets/Scripts/GamePlay/MultiPlaySystem/Component/INetSyncComponent.cs` | `GamePlay.EntitySystem` |
| `SyncConfig` | `Sync/Config/SyncConfig.cs` | `Assets/Scripts/GamePlay/MultiPlaySystem/Config/SyncConfig.cs` | `GamePlay.NetSync` |
| `TickSystem` | `Sync/Timing/TickSystem.cs` | `Assets/Scripts/GamePlay/MultiPlaySystem/TickSystem.cs` | `GamePlay.NetSync` |
| `SnapshotBuffer<T>` | `Sync/Snapshot/SnapshotBuffer.cs` | `Assets/Scripts/GamePlay/MultiPlaySystem/Snapshot/SnapshotBuffer.cs` | `GamePlay.EntitySystem` |
| `NetEntitySnapshot` | `Sync/Snapshot/NetEntitySnapshot.cs` | `Assets/Scripts/GamePlay/MultiPlaySystem/Snapshot/NetEntitySnapshot.cs` | `GamePlay.EntitySystem` |
| `NetTransformSnapshot` | `Sync/Snapshot/NetTransformSnapshot.cs` | `Assets/Scripts/GamePlay/MultiPlaySystem/Snapshot/NetTransformSnapshot.cs` | `GamePlay.EntitySystem` |
| `IEntitySnapshot` | `Sync/Snapshot/IEntitySnapshot.cs` | `Assets/Scripts/GamePlay/MultiPlaySystem/Interface/IEntitySnapshot.cs` | `GamePlay.EntitySystem` |

## Additional Note

`IEntitySnapshot` was moved with the snapshot types because `SnapshotBuffer<T>`, `NetEntitySnapshot`, and `NetTransformSnapshot` depend on it.

`NetSyncUtils` remains in `Assets/Scripts/Utils` because this project keeps global stateless helpers under `Utils`. It can later be wrapped by a sync adapter if the sync layer needs stricter protocol boundaries.

## Deferred

The following files remain in their current locations because they are behavior-heavy compatibility paths and are listed for later refactor:

- `NetPositionSync`
- `NetEntityRoleAssembler`
- `LocalController`
- `AuthorityController`
- `RemoteController`
- `NetDriverInput`
