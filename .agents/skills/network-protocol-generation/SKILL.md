---
name: network-protocol-generation
description: Regenerate network protocol code in this repository through UnityMCP and the existing Unity Editor protocol generator when protocol definitions change. Do not use for unrelated Unity Editor operations.
---

# Network Protocol Generation

Use this workflow when network protocol definitions change and generated code must be refreshed.

1. Check whether UnityMCP is connected to the intended project. If disconnected or unavailable, tell the user to connect UnityMCP to this project's Unity Editor. Leave protocol generation pending until the connection is available; continue independent work where possible.
2. Read the existing generator in `Assets/Scripts/Editor/Protobuf/ProtoCompilerWindow.cs` to identify its current generation action and settings. Its menu entry is `Tools/Framework/Proto Compiler`. Preserve the configured compiler, source selection, and output paths unless the requested change requires adjusting them.
3. Through UnityMCP, invoke the existing editor generator's generation action for the affected protocol definitions. Opening the window alone does not complete generation. Use the MCP capabilities actually available; inspect the generator's API rather than assuming a method name. If it cannot be invoked through the available UnityMCP tools, explain the limitation and leave generation pending.
4. Check the generator's reported result and Unity Console for generation or compilation errors. Report whether generation completed or remains pending. Do not add or run tests as part of this workflow.

Do not run `Protocol/protoc.exe`, another `protoc` binary, or a shell/compiler command to generate protocol code. Do not create a replacement generation script, bypass the editor generator, or hand-edit generated output as a substitute. The existing editor generator may call its configured compiler internally; that call must remain owned by the generator invoked through UnityMCP.
