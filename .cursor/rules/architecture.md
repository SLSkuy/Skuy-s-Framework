---
description: Project structure, namespace mapping, and architecture patterns
globs: Assets/Scripts/**/*.cs
alwaysApply: false
---

# Architecture

Project structure, namespaces, and patterns. C# naming and member order: `coding-style.md`.

## Project Structure

Runtime code lives under `Assets/Scripts`. Entry points `Launch.cs` and `MainEntry.cs` sit there and use the global namespace. Split `.asmdef` by responsibility; keep simulation/core independent of scene glue, gameplay hosts, transport, and tests.


| Folder            | Role                                                                                            |
| ----------------- | ----------------------------------------------------------------------------------------------- |
| `Framework/`      | `Common/`, `SubSystems/`, `Input/`, ECS `Navigation/`, `Event/`                                 |
| `GamePlay/`       | `EntitySystem/`, `MultiPlaySystem/`, `Protocol/Generated/`, `Proxy/`                            |
| `Network/`        | `Client/`, `Server/`, `Transport/` (`Kcp/`, `Tcp/`), `Config/`, `Interface/`, `Protocol/`       |
| `Events/`         | Cross-module event enums (`NetEvent`)                                                           |
| `Utils/`          | Stateless helpers (`MathUtils`, `GridUtils`, `NetUtils`, `TransformUtils`, `DataStruct/KDTree`) |
| `Editor/`         | Editor-only; keep under `Editor/` so Unity excludes them from player builds                     |
| `Core/`, `Debug/` | Supporting runtime tools                                                                        |
| `Tests/`          | Debug panels and tests; new EditMode/PlayMode tests go here or beside the owning module         |


Scenes: `Assets/Scenes` (`Dev/` for development). Do not edit `GamePlay/Protocol/Generated` unless the generator is unavailable. Third-party code: `Assets/ThirdParty`.

## Namespaces

Namespaces follow logical modules, not folder paths. Only `Launch` and `MainEntry` may use the global namespace. Reuse the owning module namespace for new types.

```csharp
// ❌ BAD — gameplay type in the global namespace
public class EntityIdleState { }

// ✅ GOOD
namespace GamePlay.EntitySystem
{
    public class EntityIdleState { }
}
```


| Namespace                  | Purpose                                   | Examples                                                       |
| -------------------------- | ----------------------------------------- | -------------------------------------------------------------- |
| `Framework`                | Services, singleton, subsystems, `Global` | `Global`, `MonoSingleton`, `SubSystemBase`, `GameStateManager` |
| `Framework.Core`           | UI controller base                        | `UIController`                                                 |
| `Framework.StateMachine`   | State machine                             | `IState`, `EnumStateBase`                                      |
| `Network`                  | Client/server and messages                | `NetClient`, `NetServer`                                       |
| `Events`                   | Cross-module event enums                  | `NetEvent`                                                     |
| `EventProcess`             | Event bus (`EventBus.Get<T>()`)           | —                                                              |
| `GamePlay.EntitySystem`    | Entities, FSM, simulation                 | `EntityCharacter`, `EntityBaseState`                           |
| `GamePlay.MultiPlaySystem` | Replication, prediction, interpolation    | `CharacterReplicationSystem`, `NetworkObjectIdentity`          |
| `Utils`                    | Stateless utilities                       | `MathUtils`                                                    |
| `NetConnect`               | Low-level connections                     | —                                                              |
| *(global)*                 | Scene entry points only                   | `Launch`, `MainEntry`                                          |




## Patterns

- **Service locator + subsystems:** inherit `SubSystemBase`, register with `Global`; `SystemManager` runs `_Init()` / `_Destroy()` ordered by `SubSystemPriority`.
- **MonoBehaviour singleton:** inherit `MonoSingleton<T>`, use `Instance`, override `Init()` / `Destroy()`, call `ShutDown()` on exit.
- **State machine:** `IState` or `EnumStateBase<TEnum>` / `ExtendableStateBase`; `Update()` calls `Tick()` and `CheckStateChange()`.
- **Event bus:** `EventBus.Get<TEvent>().Dispatch(data)` across modules; `event Action<T>` for local subscriptions.
- **Data proxy:** register `IDataProxy` via `DataProxyManager`; resolve with `Global.GetDataProxy<T>()` / `TryGetDataProxy<T>()`.
- **Network transport:** `NetClient` / `NetServer` are `SubSystemBase`; transports implement `IClientTransport` / `IServerTransport` under `Network/Transport/`; messages use Google.Protobuf.
- **Scene glue:** `EntityCharacter`, `UIController`, `NetworkObjectIdentity` only bridge Unity lifecycle; do not put reusable core logic in them.
- **Migration:** new code goes in the approved target folder/assembly. Do not extend legacy sync files or compatibility shims.

