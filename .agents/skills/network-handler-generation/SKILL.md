---
name: network-handler-generation
description: Generate network protocol Handlers in this repository through UnityMCP and the existing Unity Editor Handler Generator. Use when a task requires creating or changing protocol Handler code; do not use for unrelated gameplay logic or protocol message compilation.
---

# Network Handler Generation

Use the existing Unity Editor Handler generator through UnityMCP for network protocol Handler generation. Do not independently write or modify Handler code.

## Workflow

1. Check that UnityMCP is connected to this project's Unity Editor. If disconnected or unavailable, remind the user to connect it and leave Handler generation pending. Continue independent work where possible; do not substitute manual code changes.
2. Read `Assets/Scripts/Editor/Handler/HandlerGeneratorWindow.cs` for the current configuration and generation API. The menu entry is `Tools/Framework/Handler Generator`, and the editor type is `Framework.Editor.HandlerGeneratorWindow`.
3. Determine the class name, namespace, output directory, Handler side (`Client`, `Server`, or `RoomPlayer`), and protocol entries from the requested feature and existing protocol definitions. Each entry specifies its event ID, message type, member name, direction (`Receive`, `Send`, or `Both`), and reliability. Preserve unrelated editor settings.
4. Use available UnityMCP tools to configure and invoke the existing generator's generation action. Opening its window or viewing the preview is insufficient. Inspect the current implementation rather than assuming a public method or unsupported MCP action. Any editor-side invocation code must only configure and call the existing generator, not construct or write Handler source itself.
5. Check the generator's result and Unity Console for generation or compilation errors. Report the generated path or the reason generation remains pending. Do not add or run tests as part of this workflow.

## Boundaries

- Do not create, patch, overwrite, or hand-edit network protocol Handlers through file tools, shell commands, standalone scripts, or replacement generators. All Handler code generation must remain owned by the existing editor generator invoked through UnityMCP.
- The current generator uses `FileMode.CreateNew`; it cannot overwrite an existing Handler. If a requested change needs unsupported regeneration or modification, explain the limitation and leave that change pending. Do not delete, rename, or truncate an existing Handler to bypass this restriction, and do not modify the generator as a workaround.
- If connected UnityMCP tools cannot invoke the generator, report the limitation and leave generation pending instead of writing Handler code yourself.
- Protocol message compilation is a separate workflow covered by the independent `network-protocol-generation` skill. This skill remains separate from the general Unity MCP skill.
