# Phase 1 Checklist

## Purpose

Build the foundation only. Do not rewrite gameplay behavior yet.

## Done in This Phase

- create the `EntityControl` document structure
- move reusable base types
- define entity/controller/module boundaries
- reserve sync entry points

## Not Done Yet

- combat rewrite
- state machine rewrite
- movement rewrite
- protocol rewrite
- snapshot algorithm rewrite

## Reusable Immediately

- `NetEntityRole`
- `SyncModuleID`
- `SyncConfig`
- `TickSystem`
- `SnapshotBuffer<T>`
- `NetEntitySnapshot`
- `NetTransformSnapshot`
- `INetSyncComponent`
- `NetSyncUtils`

## Needs Refactor Before Reuse

- `EntityCharacter`
- `EntityContext`
- `EntityMotor`
- `EntityAnimator`
- `NetDriverInput`
- `LocalController`
- `AuthorityController`
- `RemoteController`
- `NetPositionSync`
- `NetEntityRoleAssembler`

## Success Criteria

- base types are organized
- responsibilities are clear
- later phases can proceed without redesigning the core again
