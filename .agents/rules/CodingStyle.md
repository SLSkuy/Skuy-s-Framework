# C# Coding Style

Detailed C# style rules. Workflow rules are defined in [AGENTS.md](../../AGENTS.md).

Use C#, 4-space indentation, and braces on their own line. Preserve CRLF (`\r\n`) in all text files and enforce it with `.gitattributes`. Public types/methods use `PascalCase`; locals/parameters use `camelCase`. Preserve existing XML summaries and inline comments, update them in place, and keep banner comments attached to their field groups.

**Preserve comments, delete dead code.** Delete replaced implementations instead of commenting them out or wrapping them in `#if false`. Keep compatibility code only when explicitly requested and mark it as legacy compatibility.

## Types

- **Classes/structs/enums:** `PascalCase`; the file name matches the primary type, one primary type per file.
- **Interfaces:** `I` prefix: `IState`, `ISubSystem`, `IUIController`, `IClientTransport`, `IDataProxy`.
- **Abstract bases:** `Base` suffix: `SubSystemBase`, `EnumStateBase`, `EntityBaseState`, `ExtendableStateBase`. Preserve generic parameters in generic bases.
- **Managers/coordinators:** `Manager` suffix: `GameStateManager`, `UIManager`, `ResourceManager`, `DataProxyManager`, `SystemManager`.
- **Static utilities:** `Utils` suffix and `static class`: `MathUtils`, `GridUtils`, `NetUtils`, `TransformUtils`.
- **Concrete states:** `State` suffix: `EntityIdleState`, `LoadingState`, `GamingState`.
- **Network components:** preserve existing names such as `NetworkObjectIdentity` and `NetworkTransformCapability`; client/server entry points remain `NetClient` / `NetServer`.

## Fields and Properties

The repository distinguishes private backing fields from serialized and public fields:

- **Private fields:** `_camelCase`, including private static fields (`_config`, `_context`, `_instance`, `_clientId`, `_fsm`, `_lastRtt`).
- **`[SerializeField] private` fields:** `camelCase` without an underscore (`animIn`, `uiControllerID`, `isVisible`, `entityId`, `role`).
- **Public fields:** `camelCase`; prefer properties when logic is involved (`public bool tickDrive;`).
- **`protected readonly` fields:** `PascalCase` (`protected readonly EntityContext Context;`).
- **Private `static readonly` collections/locks:** `PascalCase` (`Lock`, `SubSystems`).
- **Properties:** `PascalCase` (`Instance`, `Priority`, `CurrentState`, `EntityId`, `RTT`, `IsRunning`); prefer expression-bodied getters for simple accessors.

## Methods

- **Public and `protected virtual`:** `PascalCase` (`Move`, `Show`, `ChangeState`, `Init`, `Destroy`, `OnShow`, `Tick`, `CheckStateChange`).
- **Unity lifecycle:** keep Unity-defined names (`Awake`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`, `OnEnable`, `OnDisable`, `OnDestroy`, `OnAnimatorMove`).
- **Non-overridable framework hooks:** `_Init()` and `_Destroy()` only; do not invent other public underscore-prefixed APIs.
- **Async methods:** use the `Async` suffix (`LoadResourceAsync`, `LoadAsync`).
- **Wrapped parameter lists:** wrap at line width, keep multiple parameters per line, and indent continuation by 4 spaces; do not put every parameter on its own line.

  ```csharp
  // Avoid: one parameter per line
  public static void Step(
      IEntitySimulation simulation,
      EntityInputCommandBuilder commandBuilder,
      uint tick,
      float deltaTime,
      in InputState input)

  // Prefer: wrap at line width and keep several parameters per line
  public static void Step(IEntitySimulation simulation, EntityInputCommandBuilder commandBuilder,
      uint tick, float deltaTime, in InputState input)
  ```

## Member Order

Declare members in this order. Use regions only where specified and keep related fields together.

1. **Serialized fields:** all `[SerializeField]` and Inspector-visible public fields first; use `[Header]` / `[Tooltip]` and `camelCase` without an underscore.
2. **Class-reference fields:** private references such as `_config`, `_context`, `_fsm`, and `_messageProcessor`.
3. **Primitive/value fields:** `int`, `float`, `bool`, enums, and similar values; group related fields and preserve useful banners.
4. **Properties:** use `#region Properties`; prefer expression-bodied getters.
5. **Events:** use `#region Events`.
6. **Methods:** public API first, private helpers after; never region ordinary methods.
7. **Lifecycle:** put the lifecycle region at the end. Use `#region Lifecycle` for MonoBehaviour and `#region Subsystem Lifecycle` for `SubSystemBase` overrides.

`NetworkObjectIdentity.cs` is the member-order reference. `EntityCharacter.cs` contains legacy feature regions; preserve them when editing that file, but do not copy them into new code.

## File Organization

- One primary type per file; the file name matches the type.
- Use only `#region Properties`, `#region Events`, `#region Lifecycle`, and `#region Subsystem Lifecycle` for new code.
- Add XML summaries to public types, public methods, and non-obvious protected virtual methods when appropriate.
- Use `[Header]`, `[Tooltip]`, `[SerializeField]`, `[RequireComponent]`, and `[DisallowMultipleComponent]` where appropriate.
- Follow the surrounding file's choice between `var`, explicit types, and expression-bodied members.
