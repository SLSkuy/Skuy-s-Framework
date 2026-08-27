# AGENTS.md

Detailed constraints are authoritative in `.cursor/rules/`. Do not duplicate the full rules in this file. In case of conflict: current user conversation > hard constraints in this file > `.cursor/rules/`.

## Required Reading

Read only what is relevant to the task; do not scan the entire documentation set:

| Scenario | Read |
| --- | --- |
| Any runtime code modification | [`.cursor/rules/architecture.md`](.cursor/rules/architecture.md), [`.cursor/rules/coding-style.md`](.cursor/rules/coding-style.md) |
| Modifying scenes / GameObjects / running tests in the Unity Editor | [`.cursor/skills/unity-mcp-skill/SKILL.md`](.cursor/skills/unity-mcp-skill/SKILL.md) |
| Proposing / implementing / archiving an OpenSpec change | `.cursor/skills/openspec-*/SKILL.md` (`propose` / `apply` / `archive` / `explore` / `update` / `sync`) |

## Must Follow

* Modify only files required by the task. Do not perform unrelated refactoring or expand documentation beyond what is requested.
* Code conventions (C#, 4-space indent, braces, naming, member order, file organization) are owned by `.cursor/rules/coding-style.md`; project structure, the namespace mapping, and architecture patterns are owned by `.cursor/rules/architecture.md`. Do not duplicate them here.
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
.cursor/rules/                     # Cursor project rules (architecture, coding-style)
.cursor/skills/                    # Agent skills (Unity MCP, OpenSpec)
.cursor/commands/                  # OpenSpec slash commands
openspec/                          # OpenSpec root; artifacts default to Chinese, while headings and SHALL/MUST remain in English
```

Module responsibilities, namespace mappings, and architectural patterns: `architecture.md`. Type / field / method / member ordering and `#region` conventions: `coding-style.md`. Use `NetworkObjectIdentity.cs` as the reference for member ordering. Do not copy the legacy feature regions from `EntityCharacter.cs` into new files.

## OpenSpec

`openspec/config.yaml`: The default schema is `spec-driven`; artifacts are written in Chinese by default, while structural headings and SHALL/MUST remain in English.

* **propose**: Write planning artifacts only (`proposal` / `spec` / `design` / `tasks`); do not modify code in the same round.
* **apply**: Implement according to `tasks.md`; do not perform unchecked tasks.
* **archive**: Archive only after implementation is complete and verified.
