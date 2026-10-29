# Repository Guidelines

## Project Structure & Module Organization

This Unity framework uses URP, ECS, HybridCLR, and YooAsset. Runtime C# lives in `Assets/Scripts/`: `Core` and `Framework` provide infrastructure, `GamePlay` implements gameplay, and `Network`, `UI`, `Events`, and `Utils` contain supporting modules. Editor tools belong in `Assets/Scripts/Editor`. `Launch.cs` and `MainEntry.cs` are entry points.

Scenes live in `Assets/Scenes`, including `LaunchScene.unity` and development scenes under `Dev`. Prefabs, art, audio, shaders, and configuration have dedicated folders under `Assets`. Keep vendor code in `Assets/ThirdParty` separate from project changes. Dependencies and editor settings live in `Packages` and `ProjectSettings`.

## Coding Style & Naming Conventions

When editing `Assets/Scripts/**/*.cs`, read and follow [.agents/rules/coding-style.md](.agents/rules/coding-style.md). Use IDE formatting with the repository's `.editorconfig` settings.

## Development Workflow

For Unity Editor operations through MCP, use the repository skill at [.agents/skills/unity-mcp-skill/SKILL.md](.agents/skills/unity-mcp-skill/SKILL.md); load its references only as needed. Ordinary C# edits follow the coding guide above.

Code changes do not require writing or running tests. Do not add test execution or coverage requirements to the development workflow.

## Commit & Pull Request Guidelines

Recent commits use concise Chinese descriptions of changes, without a mandatory prefix. Keep commits focused and descriptive. PRs should explain behavior changes, link relevant issues, and include screenshots for visual changes.

For asset changes, read and follow [.agents/rules/unity-meta.md](.agents/rules/unity-meta.md). Commit Unity-generated `.meta` files with their assets. Exclude generated caches, local IDE files, `HybridCLRData`, and `Bundles`, following `.gitignore`.
