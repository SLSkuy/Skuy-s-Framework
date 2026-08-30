---
description: C# naming, member order, regions, and file organization
globs: Assets/Scripts/**/*.cs
alwaysApply: false
---

# C# Coding Style

C# naming, style, and member order. Folders and namespaces: `architecture.md`. Data flow and no `EnsureXxx` guards: `data-flow.md`.

Use C#, 4-space indent, braces on their own line. Types/methods: `PascalCase`. Locals/parameters: `camelCase`. Update existing XML summaries and comments in place; keep banner comments on their field groups.

**Preserve comments, delete dead code.** Remove replaced implementations; do not comment them out or wrap in `#if false`. Keep compatibility code only when requested, and mark it as legacy.

## Types

- **Classes/structs/enums:** `PascalCase`; file name matches the primary type; one primary type per file.
- **Interfaces:** `I` prefix (`IState`, `ISubSystem`, `IClientTransport`, `IDataProxy`).
- **Abstract bases:** `Base` suffix (`SubSystemBase`, `EnumStateBase`). Keep generic parameters on generic bases.
- **Managers:** `Manager` suffix (`GameStateManager`, `DataProxyManager`).
- **Static utilities:** `Utils` suffix, `static class` (`MathUtils`, `NetUtils`).
- **Concrete states:** `State` suffix (`EntityIdleState`, `LoadingState`).
- **Network names:** keep `NetworkObjectIdentity`, `CharacterPresentationAdapter`, `NetClient`, `NetServer`.



## Fields and Properties

```csharp
// ❌ BAD
private int ClientId;
[SerializeField] private int _entityId;

// ✅ GOOD
private int _clientId;
[SerializeField] private int entityId;
```

- **Private fields:** `_camelCase` (including private static: `_config`, `_instance`, `_fsm`).
- `[SerializeField] private`**:** `camelCase`, no underscore (`animIn`, `entityId`).
- **Public fields:** `camelCase`; prefer properties when there is logic (`public bool tickDrive;`).
- `protected readonly`**:** `PascalCase` (`protected readonly EntityContext Context;`).
- **Private** `static readonly` **collections/locks:** `PascalCase` (`Lock`, `SubSystems`).
- **Properties:** `PascalCase` (`Instance`, `EntityId`, `IsRunning`); expression-bodied getters for simple accessors.



## Methods

- **Public /** `protected virtual`**:** `PascalCase` (`Move`, `Init`, `Tick`, `CheckStateChange`).
- **Unity lifecycle:** keep Unity names (`Awake`, `Update`, `OnDestroy`, …).
- **Framework hooks:** only `_Init()` and `_Destroy()`; do not add other public underscore APIs.
- **Async:** `Async` suffix (`LoadResourceAsync`).
- **Parameter wrap:** wrap at line width, several parameters per line, continuation indent 4 spaces. Do not put one parameter per line.

```csharp
// ❌ BAD
public static void Step(
    IEntitySimulation simulation,
    EntityInputCommandBuilder commandBuilder,
    uint tick,
    float deltaTime,
    in InputState input)

// ✅ GOOD
public static void Step(IEntitySimulation simulation, EntityInputCommandBuilder commandBuilder,
    uint tick, float deltaTime, in InputState input)
```



## Member Order

Keep related fields together. Regions only where listed.

1. Serialized / Inspector-visible fields (`[Header]` / `[Tooltip]`, `camelCase`, no underscore)
2. Class-reference fields (`_config`, `_context`, `_fsm`)
3. Primitive/value fields (`int`, `float`, `bool`, enums); keep useful banners
4. Properties — `#region 属性`
5. Events — `#region 事件`
6. Methods — public API first, private helpers after; do not region ordinary methods
7. Lifecycle last — `#region 生命周期` (MonoBehaviour) or `#region 子系统生命周期` (`SubSystemBase`)

Reference: `NetworkObjectIdentity.cs`. Preserve legacy feature regions in `EntityCharacter.cs`; do not copy them into new files.

## File Organization

- One primary type per file; file name matches the type.
- New code may use only `#region 属性`, `#region 事件`, `#region 生命周期`, `#region 子系统生命周期`.
- XML summaries on public types, public methods, and non-obvious `protected virtual` methods when useful.
- Use `[Header]`, `[Tooltip]`, `[SerializeField]`, `[RequireComponent]`, `[DisallowMultipleComponent]` where they fit.
- Match the surrounding file for `var` vs explicit types and expression-bodied members.

