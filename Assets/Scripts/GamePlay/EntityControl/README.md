# EntityControl Runtime Layout

This folder is the target runtime container for the entity control refactor.

The namespace remains `GamePlay.EntitySystem` unless a later reviewed step explicitly changes it.

## Folders

- `Core/` - narrow contracts, base lifecycle seams, shared entity state views.
- `Entities/` - `BaseEntity` and derived entity types such as player, remote player, NPC, monster, vehicle, and interactable entities.
- `Controllers/` - control-source orchestrators that route input or authority intent into entity contracts.
- `Modules/` - attachable entity capability modules such as movement, animation, interaction, health, camera target, and physics proxy.
- `Sync/` - sync-facing contracts and adapters for state capture, role dispatch, prediction, replay, interpolation, and animation sync.

## Migration Notes

- Existing code under `EntitySystem/Character` and `MultiPlaySystem/Component` remains the compatibility source until each reviewed phase migrates behavior. Controller and submodule reusable code has moved into `EntityControl`.
- The empty `EntitySystem/EntityControl` folder is not used as the target layout. It is left untouched for now and can be cleaned in Phase 7 after Unity references are confirmed.
