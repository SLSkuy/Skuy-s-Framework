# Architecture

This document describes the project structure and established patterns.

## Project Structure and Modules

This is a Unity project. Runtime code lives under `Assets/Scripts`; scene entry points `Launch.cs` and `MainEntry.cs` are directly under `Assets/Scripts/` and use the global namespace. Runtime code may be split into `.asmdef` assemblies by responsibility. Keep dependencies explicit and keep pure simulation/core assemblies independent from Unity scene glue, gameplay hosts, transport, and tests.

Top-level modules under `Assets/Scripts/`:

- `Framework/` — reusable core framework: `Common/`, `SubSystems/`, `Input/`, ECS-based `Navigation/`, and `Event/`.
- `GamePlay/` — gameplay code: `EntitySystem/`, `MultiPlaySystem/`, generated `Protocol/Generated/`, and `Proxy/`.
- `Network/` — networking: `Client/`, `Server/`, `Transport/` (`Kcp/`, `Tcp/`), `Config/`, `Interface/`, and `Protocol/`.
- `Events/` — cross-module event enums such as `NetEvent`.
- `Utils/` — stateless utilities such as `MathUtils`, `GridUtils`, `NetUtils`, `TransformUtils`, and `DataStruct/KDTree`.
- `Editor/` — editor-only tools. Editor scripts must stay under `Editor/` so Unity excludes them from builds.
- `Core/`, `Debug/` — supporting runtime tools.
- `Tests/` — runtime debug panels and tests; new EditMode/PlayMode tests go here or beside the owning module.

Scenes live under `Assets/Scenes`, with development scenes under `Assets/Scenes/Dev`. Generated protocol classes live under `Assets/Scripts/GamePlay/Protocol/Generated`; do not edit them unless the generator is unavailable. Third-party code lives under `Assets/ThirdParty`.

## Namespace Conventions

Namespaces represent logical modules and do **not** strictly mirror folder paths. Use established names:


| Namespace                  | Purpose                                                | Examples                                                                   |
| -------------------------- | ------------------------------------------------------ | -------------------------------------------------------------------------- |
| `Framework`                | Core services, singleton, subsystems, `Global` locator | `Global.cs`, `MonoSingleton.cs`, `SubSystemBase.cs`, `GameStateManager.cs` |
| `Framework.Core`           | UI controller base and shared UI core                  | `UIController.cs`                                                          |
| `Framework.StateMachine`   | General state machine                                  | `IState.cs`, `EnumStateBase.cs`                                            |
| `Network`                  | Client/server networking and message handling          | `NetClient.cs`, `NetServer.cs`                                             |
| `Events`                   | Cross-module event enums                               | `NetEvent.cs`                                                              |
| `EventProcess`             | Event bus (`EventBus.Get<T>()`)                        | —                                                                          |
| `GamePlay.EntitySystem`    | Entity characters, FSM states, simulation              | `EntityCharacter.cs`, `EntityBaseState.cs`                                 |
| `GamePlay.MultiPlaySystem` | Character replication, prediction, interpolation       | `CharacterReplicationSystem.cs`, `NetworkObjectIdentity.cs`                |
| `Utils`                    | Stateless utilities                                    | `MathUtils.cs`                                                             |
| `NetConnect`               | Low-level connection primitives                        | —                                                                          |
| *(global)*                 | Scene entry points only                                | `Launch.cs`, `MainEntry.cs`                                                |


Only `Launch` and `MainEntry` may use the global namespace. All other types must declare a module namespace. Reuse the owning module namespace for new subsystems.

## Architecture Patterns

Match these established patterns before writing framework-facing code:

- **Service locator + subsystems:** framework services inherit `SubSystemBase` and register with `Global`; `SystemManager` drives `_Init()` / `_Destroy()` and orders subsystems by `SubSystemPriority`.
- **MonoBehaviour singleton:** inherit `MonoSingleton<T>`, access through `Instance`, override `Init()` / `Destroy()`, and call `ShutDown()` on application exit.
- **State machine:** implement `IState` or inherit `EnumStateBase<TEnum>` / `ExtendableStateBase`; `Update()` calls `Tick()` and `CheckStateChange()`.
- **Event bus:** dispatch cross-module events with `EventBus.Get<TEvent>().Dispatch(data)`; use `event Action<T>` for local subscriptions.
- **Data proxy:** register `IDataProxy` instances through `DataProxyManager` and retrieve them through `Global.GetDataProxy<T>()` / `Global.TryGetDataProxy<T>()`.
- **Network transport:** `NetClient` and `NetServer` are `SubSystemBase`; transports implement `IClientTransport` / `IServerTransport`; KCP/TCP variants live under `Network/Transport/`; messages use Google.Protobuf.
- **Scene glue vs reusable services:** `EntityCharacter`, `UIController`, and `NetworkObjectIdentity` bridge Unity lifecycle to services and must not contain reusable core logic.
- **Migration:** once a module has an approved target folder/assembly, put new code there. Do not extend legacy sync files or compatibility shims with new behavior.

