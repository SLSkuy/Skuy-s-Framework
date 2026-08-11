# EntityControl Runtime Layout

This folder is the target runtime container for the entity control refactor.

The namespace remains `GamePlay.EntitySystem` unless a later reviewed step explicitly changes it.

## Folders

- `Core/` - narrow contracts, base lifecycle seams, shared entity state views.
- `Entities/` - `BaseEntity`, `EntityCharacter`, entity context/config, FSM, and derived entity types such as player, remote player, NPC, monster, and interactable entities.
- `Controllers/` - control-source orchestrators that route input or authority intent into entity contracts.
- `Modules/` - attachable entity capability modules such as movement, animation, interaction, health, camera target, and physics proxy.
- `Sync/` - sync-facing contracts and adapters for state capture, role dispatch, prediction, replay, interpolation, and animation sync.

## Migration Notes

- Entity, controller, module, and sync runtime code has moved into `EntityControl`.
- Legacy empty folders under `EntitySystem` and `MultiPlaySystem/Component` were removed during cleanup.
