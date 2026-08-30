# AGENTS.md

Detailed constraints are authoritative in `.cursor/rules/`. Do not duplicate the full rules in this file. In case of conflict: current user conversation > hard constraints in this file > `.cursor/rules/`.

## Required Reading

Read only what is relevant to the task; do not scan the entire documentation set:

| Scenario | Read |
| --- | --- |
| Any runtime code modification | [`.cursor/rules/architecture.md`](.cursor/rules/architecture.md), [`.cursor/rules/coding-style.md`](.cursor/rules/coding-style.md), [`.cursor/rules/data-flow.md`](.cursor/rules/data-flow.md) |
| Modifying scenes / GameObjects in the Unity Editor | [`.cursor/skills/unity-mcp-skill/SKILL.md`](.cursor/skills/unity-mcp-skill/SKILL.md) |

## Must Follow

* Modify only files required by the task. Do not perform unrelated refactoring or expand documentation beyond what is requested.
* Code conventions (C#, 4-space indent, braces, naming, member order, file organization) are owned by `.cursor/rules/coding-style.md`; project structure, the namespace mapping, and architecture patterns are owned by `.cursor/rules/architecture.md`; data-flow and the ban on `EnsureXxx`-style defensive code are owned by `.cursor/rules/data-flow.md`. Do not duplicate them here.
* Do not manually modify `Assets/Scripts/GamePlay/Protocol/Generated/`. Modify the `.proto` files under `Assets/Scripts/Network/Protocol/`, then regenerate the code using the Proto compilation window in the Unity Editor.
* Do not modify `Assets/ThirdParty/` unless explicitly required by the task.
* After modifying scripts: wait for Unity compilation to finish, then check the Console for errors. When using Unity MCP, read the relevant resource first, then invoke tools.

## Repository Map

```text
Assets/Scripts/
  Launch.cs, MainEntry.cs          # Scene entry points (global namespace)
  Framework/                       # Reusable core: Common, SubSystems, Input, Navigation, Event
  GamePlay/                        # Gameplay: EntitySystem, MultiPlaySystem, Protocol/Generated, Proxy
  Network/                         # Client, Server, Transport (Kcp/Tcp), Config, Interface, Protocol
  Events/                          # Cross-module event enums
  Utils/                           # Stateless utilities
  Editor/                          # Editor-only
  Core/, Debug/, Tests/
Assets/Scenes/                     # LaunchScene, GameScene; development scenes under Scenes/Dev
.cursor/rules/                     # Cursor project rules (architecture, coding-style, data-flow)
.cursor/skills/                    # Agent skills (Unity MCP)
```

Module responsibilities, namespace mappings, and architectural patterns: `architecture.md`. Type / field / method / member ordering and `#region` conventions: `coding-style.md`. Data flow and no over-protective guards: `data-flow.md`. Use `NetworkObjectIdentity.cs` as the reference for member ordering. Do not copy the legacy feature regions from `EntityCharacter.cs` into new files.
