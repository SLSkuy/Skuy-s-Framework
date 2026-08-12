# Repository Guidelines

## Project Structure & Module Organization
This is a Unity project. Runtime source lives in `Assets/Scripts`, with the scene entry points `Launch.cs` and `MainEntry.cs` placed directly under `Assets/Scripts/` (both in the global namespace, no `namespace` declaration). The whole `Assets/Scripts` tree compiles into the default `Assembly-CSharp` — there are currently no `.asmdef` files, so treat all runtime code as one assembly and respect module boundaries by namespace discipline rather than assembly references.

Top-level modules under `Assets/Scripts/`:

- `Framework/` — reusable core framework. `Common/` (singleton, state machine, subsystem base), `SubSystems/` (Camera, DataProxy, ObjectPool, Resource, SceneControl, States, Timer, UI), `Input/`, `Navigation/` (A* over ECS), `Event/`.
- `GamePlay/` — gameplay code. `EntitySystem/` (Character, Config, Controller, FSM, SubModule), `MultiPlaySystem/` (Component, Config, Interface, Snapshot), `Protocol/Generated/` (Protobuf-generated, do not hand-edit), `Proxy/`.
- `Network/` — networking. `Client/`, `Server/`, `Transport/` (`Kcp/`, `Tcp/`), `Config/`, `Interface/`, `Protocol/`.
- `Events/` — cross-module event enums (e.g. `NetEvent`).
- `Utils/` — stateless static helpers (`MathUtils`, `GridUtils`, `NetUtils`, `TransformUtils`, plus `DataStruct/KDTree`).
- `Editor/` — editor-only tooling (`Protobuf/`, `UIFramework/`). Editor scripts must sit under an `Editor/` folder so Unity excludes them from builds.
- `Core/`, `Debug/` — auxiliary runtime helpers.
- `Tests/` — currently holds runtime debug panels (e.g. `NetworkTestPanel.cs`); place new EditMode/PlayMode unit tests here or beside their module.

Scenes live in `Assets/Scenes`, with development scenes under `Assets/Scenes/Dev`. Generated protocol classes live in `Assets/Scripts/GamePlay/Protocol/Generated`; avoid hand-editing generated files unless the generator is unavailable. Third-party code is kept in `Assets/ThirdParty`. Design documents live in the root-level `Docs/` folder (not under `Assets/`) — see [Documentation Guidelines](#documentation-guidelines) below.

## Namespace Conventions
Namespaces are logical module names — they do **not** strictly mirror the folder path. Use the established names:

| Namespace | Used for | Example |
| --- | --- | --- |
| `Framework` | Core framework services, singletons, subsystem base, `Global` locator | `Global.cs`, `MonoSingleton.cs`, `SubSystemBase.cs`, `GameStateManager.cs` |
| `Framework.Core` | UI controller base and shared UI core | `UIController.cs` |
| `Framework.StateMachine` | Generic state machine (`IState`, `EnumStateBase<T>`) | `IState.cs`, `EnumStateBase.cs` |
| `Network` | Client/server networking, transports, message processing | `NetClient.cs`, `NetServer.cs` |
| `Events` | Cross-module event enums | `NetEvent.cs` |
| `EventProcess` | Event bus (`EventBus.Get<T>()`) | — |
| `GamePlay.EntitySystem` | Entity character, FSM states, network identity | `EntityCharacter.cs`, `EntityBaseState.cs`, `NetEntityIdentity.cs` |
| `Utils` | Stateless static helpers | `MathUtils.cs` |
| `NetConnect` | Low-level connection primitives | — |
| *(global)* | Scene entry points only | `Launch.cs`, `MainEntry.cs` |

Only `Launch` and `MainEntry` may live in the global namespace; everything else must declare a module namespace. When adding a subsystem, reuse the owning module's namespace rather than inventing a new one.

## Architecture & Patterns
Before writing framework-facing code, match these established patterns:

- **Service locator + subsystems.** Framework services extend `SubSystemBase` and self-register into the static `Global` locator (see `Framework/Common/SubSystemManager/`). Business code retrieves them via `Global.Get<T>()` / `Global.TryGet<T>(out var s)`. Lifecycle is driven by the non-overridable `_Init()` / `_Destroy()` hooks; subclasses override the virtual `Init()` / `Destroy()` / `BindEvents()` / `Update()` / `FixedUpdate()` / `LateUpdate()` instead. Each subsystem exposes a unique `SubSystemPriority`.
- **MonoBehaviour singletons.** Derive from `MonoSingleton<T>`; access via `Instance`, override `Init()` / `Destroy()`, and call `ShutDown()` on application quit.
- **State machines.** States implement `IState` (or extend `EnumStateBase<TEnum>` / `ExtendableStateBase`). `Update()` calls `Tick()` then `CheckStateChange()`; override those rather than `Update`. Concrete states use the `...State` suffix (e.g. `EntityIdleState`, `MainMenuState`).
- **Event bus.** Dispatch cross-module events with `EventBus.Get<TEvent>().Dispatch(data)`; use `event Action<T>` for direct subscriptions within a class.
- **Data proxies.** `IDataProxy` instances are registered through `DataProxyManager` and accessed via `Global.GetDataProxy<T>()` / `Global.TryGetDataProxy<T>()`.
- **Network transport.** `NetClient` and `NetServer` are both `SubSystemBase`; transports implement `IClientTransport` / `IServerTransport` with KCP and TCP variants under `Network/Transport/`. Messages use Google.Protobuf; generated messages live in `GamePlay/Protocol/Generated/`.
- **Keep MonoBehaviour scene glue separate from reusable services.** MonoBehaviours (`EntityCharacter`, `UIController`, `NetEntityIdentity`) wire Unity lifecycle to framework services; they should not contain reusable logic that belongs in a `SubSystemBase`.

## Coding Style & Naming Conventions
Use C# with 4-space indentation and braces on their own line. **Line endings are CRLF (`\r\n`)** across the repository — keep new and edited text files (`.cs`, `.md`, `.json`, `.asmdef`, `.txt`, shader files, etc.) in CRLF, and do not convert existing files to LF. A `.gitattributes` with `* text=auto eol=crlf` is the recommended way to enforce this repo-wide. Public types and methods use `PascalCase`; local variables and parameters use `camelCase`. The codebase uses Chinese XML doc summaries and inline comments — match that when extending existing files, and **do not arbitrarily delete existing comments**. When refactoring, preserve and update comments in place; remove only what is clearly obsolete, and keep comment banners (e.g. `// ========== 心跳 ==========`) with the field group they describe.

**Comments are preserved, but dead code is removed.** When refactoring, delete the old implementation outright — do not leave it commented out, `#if false`'d, or sitting unused. Keep the file tidy and let the new code stand on its own. Retain old code only when explicitly told to keep it for backward compatibility, in which case mark the seam clearly (e.g. an `// 旧实现，保留兼容` banner) so the intent is visible.

### Types
- **Classes/structs/enums:** `PascalCase`. File name matches the primary type name; one primary type per file.
- **Interfaces:** `I` prefix — `IState`, `ISubSystem`, `IUIController`, `IClientTransport`, `IDataProxy`.
- **Abstract bases:** `Base` suffix — `SubSystemBase`, `EnumStateBase`, `EntityBaseState`, `ExtendableStateBase`. Generic bases keep the type parameter — `MonoSingleton<T>`, `UIController<T>`, `EnumStateBase<TEnum>`.
- **Managers/coordinators:** `Manager` suffix — `GameStateManager`, `UIManager`, `ResourceManager`, `DataProxyManager`, `SystemManager`.
- **Static helpers:** `Utils` suffix, `static class` — `MathUtils`, `GridUtils`, `NetUtils`, `TransformUtils`.
- **Concrete states:** `State` suffix — `EntityIdleState`, `LoadingState`, `GamingState`.
- **Network components:** `Net` prefix for network-facing MonoBehaviours — `NetEntityIdentity`; client/server entry points are `NetClient` / `NetServer`.

### Fields & properties
The codebase distinguishes private backing fields from serialized/public fields:

- **Private fields:** `_camelCase` — `_config`, `_context`, `_instance`, `_clientId`, `_fsm`, `_lastRtt`. Private static fields also `_camelCase` (`_applicationIsQuitting`).
- **`[SerializeField] private` fields:** `camelCase` **without** underscore — `animIn`, `uiControllerID`, `isVisible`, `entityId`, `role`. (Underscore is dropped so the Inspector name stays clean.)
- **Public fields:** `camelCase` — `public bool tickDrive;`. Prefer properties over public fields where logic is involved.
- **`protected readonly` fields:** `PascalCase` — `protected readonly EntityContext Context;`.
- **Private `static readonly` collections/locks:** `PascalCase` — `Lock`, `SubSystems`.
- **Properties:** `PascalCase` — `Instance`, `Priority`, `CurrentState`, `EntityId`, `RTT`, `IsRunning`. Prefer expression-body for trivial getters (`public uint EntityId => entityId;`).

### Methods
- **Public & protected virtual:** `PascalCase` — `Move`, `Show`, `ChangeState`, `Init`, `Destroy`, `OnShow`, `Tick`, `CheckStateChange`.
- **Unity lifecycle:** `Awake`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`, `OnEnable`, `OnDisable`, `OnDestroy`, `OnAnimatorMove`.
- **Framework-internal non-overridable hooks:** underscore prefix — `_Init()`, `_Destroy()` (declared non-virtual on `SubSystemBase`). Do not add new underscore-prefixed public API casually; reserve it for base-class lifecycle seams.
- **Async methods:** `Async` suffix — `LoadResourceAsync`, `LoadAsync`.

### Member declaration order
Within a class or struct, declare members top-to-bottom in this fixed order. Wrap each group in a `#region` where indicated, and group functionally related fields together inside a group.

1. **Serialized fields** — all `[SerializeField]` (and any `public` Inspector-facing) fields first, using `[Header]` / `[Tooltip]` to group and describe them. `camelCase` without underscore.
2. **Class-typed reference fields** — private references to other class/instance types, e.g. `_config`, `_context`, `_fsm`, `_messageProcessor` (`_camelCase`).
3. **Primitive / value fields** — `int`, `float`, `bool`, enums, etc., e.g. `_clientId`, `_token`, `_tryReconnect`. Group fields that serve one feature together; a comment banner like `// ========== 网络心跳 ==========` (as in `NetClient`) is the established way to label a sub-group.
4. **Properties** — wrap in `#region 属性`. Prefer expression-body for trivial getters (`public uint EntityId => entityId;`).
5. **Events** — wrap in `#region 事件` (`public event Action<T> ...`, `event Action<IUIController>`).
6. **Methods** — declare in order, public API first, then private helpers. **Do not wrap methods or method groups in `#region`**; `#region` is reserved for the three cases named above (`属性`, `事件`, `生命周期`).
7. **Unity / lifecycle methods** — wrap in `#region 生命周期` (or `#region Unity生命周期`) and place at the **end of the file**. This covers `Awake`, `Start`, `OnEnable`, `OnDisable`, `Update`, `FixedUpdate`, `LateUpdate`, `OnDestroy`, `OnAnimatorMove`, etc.

Reference implementation: [NetEntityIdentity.cs](file:///d:/Project/Unity_Project/Skuy's%20Framework/Assets/Scripts/GamePlay/MultiPlaySystem/Component/NetEntityIdentity.cs) (serialized fields → `#region 属性` → `#region 事件` → methods, with no method regions) is the model to follow. [EntityCharacter.cs](file:///d:/Project/Unity_Project/Skuy's%20Framework/Assets/Scripts/GamePlay/EntitySystem/Character/EntityCharacter.cs) still carries legacy feature-named regions (`#region 实体控制`, `#region 模拟入口`); do not replicate those in new code — only its `#region 属性` and `#region 生命周期` match the current rule.

### File organization
- One primary type per file; file name matches the type.
- Apply the member order above; do not interleave fields, properties, and methods out of order. **Only three `#region` labels are used: `#region 属性`, `#region 事件`, `#region 生命周期`.** Do not wrap regular methods or any other member group in `#region`. When editing legacy files that already contain extra feature-named regions (e.g. `#region 实体控制`, `#region 模拟入口`), leave them in place rather than deleting, but do not introduce new ones.
- Add `/// <summary>` XML docs (Chinese is fine) on public types, public methods, and non-obvious protected virtuals.
- Use Unity attributes for Inspector-facing fields: `[Header]`, `[Tooltip]`, `[SerializeField]`; use `[RequireComponent]` / `[DisallowMultipleComponent]` on MonoBehaviours that depend on a sibling component or must be unique.
- Use `var` or explicit types as the surrounding file does; both appear. Keep expression-body members where the file already uses them.

## Build, Test, and Development Commands
Open the project with Unity Hub or the Unity Editor version configured for this workspace. For command-line checks, use Unity batch mode:

```powershell
Unity.exe -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml
Unity.exe -batchmode -quit -projectPath . -runTests -testPlatform PlayMode -testResults PlayModeResults.xml
Unity.exe -batchmode -quit -projectPath . -buildTarget Win64
```

Use the Editor for scene validation and package restoration. Commit `Assets`, `Packages`, and `ProjectSettings`; do not commit generated local folders such as `Library`, `Temp`, `Logs`, `obj`, or `UserSettings`.

## Testing Guidelines
The project includes Unity Test Framework. Place edit-mode or play-mode tests near the relevant module or under `Assets/Scripts/Tests`. Name test files and methods after the behavior being verified, for example `SnapshotBufferTests` or `StoresSnapshotsInTickOrder`. Run both EditMode and PlayMode tests before merging gameplay, networking, ECS navigation, or resource-management changes.

## Documentation Guidelines
Design documents live in the root-level `Docs/` folder (not inside `Assets/`), organized one subfolder per feature. Follow the layout already established by `Docs/EntityControl/`:

- **One folder per feature**, named after the feature (e.g. `Docs/EntityControl/`, `Docs/Navigation/`). Put all docs for that feature inside its folder; do not scatter them across the repo.
- **Numbered file prefix** for reading order — `00-Index.md`, `01-Design-Baseline.md`, `02-Phase-1-Checklist.md`, … Filenames stay English `kebab-case` (ASCII, hyphen-separated) so they remain git- and cross-platform-friendly.
- **An index file** `00-Index.md` per feature folder, listing the documents and their reading order; update it whenever a new doc is added.
- **Write content in Chinese (中文).** Headings, prose, lists, and explanations are all Chinese; keep code symbols, type names, identifiers, and file paths in their original form. New documents must be Chinese. (The pre-existing `Docs/EntityControl/` set was written in English — leave it as-is unless explicitly rewriting it.)
- **Make documents self-contained and complete.** Each design doc must carry its full context so it can be understood with zero prior conversation history. Concretely state: (1) **data sources** — where data is read from (e.g. a config file path, a `ScriptableObject` directory, a specific protobuf message, a subsystem fetched via `Global.Get<T>()`); (2) **modules involved** — which namespace/folder/class owns the behavior today and where the new code should live (give the exact folder and namespace); (3) **required changes** — which files to add or modify, which interfaces to implement, which call sites to update, and the expected post-change behavior. Do not write thin or vague docs that only sketch an idea — spell out 数据从哪来 / 模块在哪 / 改动哪些地方 explicitly. This ensures that switching to a fresh conversation (with no shared prior context) still yields correct, unambiguous implementation rather than misidentification.
- **When to write one.** Create a design doc under `Docs/<Feature>/` when a change involves a new subsystem, a cross-module refactor, a protocol addition, or an architecture decision. Link it from that folder's `00-Index.md` and commit the doc alongside the code it describes.

## Commit & Pull Request Guidelines
Recent history uses concise Conventional Commit-style messages such as `feat(entity): ...` and `refactor(entity system): ...`; keep using `type(scope): summary`. English and Chinese summaries both appear in history, but the scope and type should stay clear. Pull requests should include a short description, affected scenes or systems, test results, and screenshots or short recordings for UI, animation, scene, or gameplay-visible changes.

## Agent-Specific Instructions
Before editing, inspect the owning module and preserve its existing patterns — namespace, region layout, member declaration order, comment language, and field-naming flavor (`_camelCase` private vs `camelCase` serialized). Do not delete or strip existing comments when refactoring; update them in place. Conversely, delete old/superseded code outright when refactoring — do not leave it commented out or unused; keep files clean, and retain old code only when explicitly asked to keep it for compatibility. Do not rewrite generated protocol files (`GamePlay/Protocol/Generated/`) or third-party packages unless explicitly requested. Do not introduce `.asmdef` files to split the assembly without explicit approval — the project intentionally ships as a single `Assembly-CSharp`. Preserve CRLF line endings when editing or creating files; do not normalize to LF, and ensure newly written files are saved as CRLF. When changing scripts, let Unity recompile and check the console for errors before considering the task complete.
