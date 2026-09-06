---
description: Project structure, namespace mapping, and architecture patterns
globs: Assets/Scripts/**/*.cs
alwaysApply: false
---

# Architecture

Project structure, namespaces, and patterns. C# naming, member order, and data-flow guards: `coding-style.md`.

## Project Structure

Runtime code lives under `Assets/Scripts`. Entry points `Launch.cs` and `MainEntry.cs` sit there and use the global namespace. Split `.asmdef` by responsibility; keep simulation/core independent of scene glue, gameplay hosts, transport, and tests.


| Folder            | Role                                                                                            |
| ----------------- | ----------------------------------------------------------------------------------------------- |
| `Framework/`      | `Common/`, `SubSystems/`, `Input/`, ECS `Navigation/`, `Event/`                                 |
| `GamePlay/`       | `Procedure/`, `Battle/`, `GameSession/`, `Simulator/`, `EntitySystem/`, `MultiPlaySystem/`, `Protocol/Generated/` |
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
| `Framework`                | Services, singleton, subsystems, `Global` | `Global`, `MonoSingleton`, `SubSystemBase`, `LocalInputManager` |
| `Framework.Core`           | UI controller base                        | `UIController`                                                 |
| `Framework.StateMachine`   | State machine                             | `IState`, `EnumStateBase`                                      |
| `Network`                  | Client/server and messages                | `NetClient`, `NetServer`                                       |
| `Events`                   | Cross-module event enums                  | `NetEvent`                                                     |
| `GamePlay.Procedure`       | 玩法流程（菜单/大厅/对局）                 | `ProcedureManager`, `GameProcedure`                            |
| `GamePlay.Battle`          | 战局会话                                  | `BattleManager`, `BattleRoom`                                  |
| `GamePlay.EntitySystem`    | Entities, FSM, simulation                 | `EntityCharacter`, `EntityBaseState`                           |
| `GamePlay.MultiPlaySystem` | Replication, prediction, interpolation    | `CharacterReplicationSystem`, `NetworkObjectIdentity`          |
| `Utils`                    | Stateless utilities                       | `MathUtils`                                                    |
| `NetConnect`               | Generated connect protocol                | —                                                              |
| *(global)*                 | Scene entry points only                   | `Launch`, `MainEntry`                                          |

## Composition and lifetimes

- **HybridCLR:** `Launch` (AOT) loads the hot-update assembly and invokes `MainEntry.Run`. `MainEntry` only switches to `MainScene`; it MUST NOT register gameplay systems.
- **Shell (`GameCore`):** composition root for process-lifetime Framework modules only (resource, pool, timer, data proxy, scene, local input, UI, camera). Do not register `BattleManager`, `GameManager`, simulation kernels, `NetServer`, or `NetClient`. Do not reference `GamePlay` types.
- **Procedure (`GamePlay.Procedure`):** a scene object beside `GameCore` (`ProcedureManager` MonoBehaviour). It owns Menu → Lobby → Match and ticks itself. UI uses `ProcedureManager.Instance` (`StartLocal` / `HostMultiplayer` / `JoinRemote` / `RequestStartMatch` / `LeaveSession`). UI MUST NOT `Global.Get<BattleManager>()`.
- **Three lifetimes:** process = `GameCore` register list; session = Lobby registers `BattleManager` (and network via battle APIs), Menu unregisters it; match = `BattleRoom.StartMatch` / `EndMatch` for `GameManager` and kernels. `BattleRoom` is not a subsystem.
- **Assemblies:** `Skuy.Core` → `Skuy.Framework` only. `Skuy.Framework` and `Skuy.Core` MUST NOT reference `Skuy.GamePlay`. `Skuy.GamePlay` → Framework + Network (+ Events/Utils/Protocol). `Skuy.Tests` is debug-only and is not the session owner.

## Patterns

- **Service locator + subsystems:** inherit `SubSystemBase`, register with `Global`; `SystemManager` runs `_Init()` / `_Destroy()` ordered by `SubSystemPriority`.
- **MonoBehaviour singleton:** inherit `MonoSingleton<T>`, use `Instance`, override `Init()` / `Destroy()`, call `ShutDown()` on exit.
- **State machine:** `IState` or `EnumStateBase<TEnum>` / `ExtendableStateBase`; `Update()` calls `Tick()` and `CheckStateChange()`.
- **Event bus:** `EventBus.Get<TEvent>().Dispatch(data)` across modules; `event Action<T>` for local subscriptions.
- **Data proxy:** register `IDataProxy` via `DataProxyManager`; resolve with `Global.GetDataProxy<T>()` / `TryGetDataProxy<T>()`.
- **Network transport:** `NetClient` / `NetServer` are `SubSystemBase`; transports implement `IClientTransport` / `IServerTransport` under `Network/Transport/`; messages use Google.Protobuf.
- **Scene glue:** `EntityCharacter`, `UIController`, `NetworkObjectIdentity` only bridge Unity lifecycle; do not put reusable core logic in them.
- **Migration:** new code goes in the approved target folder/assembly. Do not extend legacy sync files or compatibility shims.
