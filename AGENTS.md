# Repository Guidelines

## Project Structure & Module Organization
This is a Unity project. Runtime source lives in `Assets/Scripts`, with the main entry points in `Launch.cs` and `MainEntry.cs`. Core framework code is under `Assets/Scripts/Framework`, gameplay code under `Assets/Scripts/GamePlay`, networking under `Assets/Scripts/Network`, editor tools under `Assets/Scripts/Editor`, and utility code under `Assets/Scripts/Utils`. Scenes are in `Assets/Scenes`, with development scenes grouped under `Assets/Scenes/Dev`. Generated protocol classes live in `Assets/Scripts/GamePlay/Protocol/Generated`; avoid hand-editing generated files unless the generator is unavailable. Third-party code is kept in `Assets/ThirdParty`.

## Build, Test, and Development Commands
Open the project with Unity Hub or the Unity Editor version configured for this workspace. For command-line checks, use Unity batch mode:

```powershell
Unity.exe -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml
Unity.exe -batchmode -quit -projectPath . -runTests -testPlatform PlayMode -testResults PlayModeResults.xml
Unity.exe -batchmode -quit -projectPath . -buildTarget Win64
```

Use the Editor for scene validation and package restoration. Commit `Assets`, `Packages`, and `ProjectSettings`; do not commit generated local folders such as `Library`, `Temp`, `Logs`, `obj`, or `UserSettings`.

## Coding Style & Naming Conventions
Use C# with 4-space indentation and Unity conventions. Public types and methods use `PascalCase`; private fields commonly use `_camelCase`; local variables and parameters use `camelCase`. Keep namespaces aligned with the owning module, for example `Framework`, `Network`, `Events`, or `UIFramework`. Prefer explicit subsystem boundaries over cross-module shortcuts, and keep MonoBehaviour scene glue separate from reusable framework services.

## Testing Guidelines
The project includes Unity Test Framework. Place edit-mode or play-mode tests near the relevant module or under a clear `Tests` folder. Name test files and methods after the behavior being verified, for example `SnapshotBufferTests` or `StoresSnapshotsInTickOrder`. Run both EditMode and PlayMode tests before merging gameplay, networking, ECS navigation, or resource-management changes.

## Commit & Pull Request Guidelines
Recent history uses concise Conventional Commit-style messages such as `feat(entity): ...` and `refactor(entity system): ...`; keep using `type(scope): summary`. English and Chinese summaries both appear in history, but the scope and type should stay clear. Pull requests should include a short description, affected scenes or systems, test results, and screenshots or short recordings for UI, animation, scene, or gameplay-visible changes.

## Agent-Specific Instructions
Before editing, inspect the owning module and preserve its existing patterns. Do not rewrite generated protocol files or third-party packages unless explicitly requested. When changing scripts, let Unity recompile and check the console for errors before considering the task complete.
