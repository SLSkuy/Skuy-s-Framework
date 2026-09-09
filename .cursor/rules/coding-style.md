---
description: C# coding style and data-flow rules; avoid over-defensive code and silent recovery of broken invariants.
alwaysApply: true
---

# C# Coding Style

C# naming, style, member order, and data-flow guards. Folders, namespaces, and patterns: `architecture.md`.

Use C#, 4-space indent, braces on their own line. Types/methods: `PascalCase`. Locals/parameters: `camelCase`. Update existing XML summaries and comments in place; keep banner comments on their field groups.

**Preserve comments, delete dead code.** Remove replaced implementations; do not comment them out or wrap in `#if false`. Keep compatibility code only when requested, and mark it as legacy.

## Types

* **Classes/structs/enums:** `PascalCase`; file name matches the primary type; one primary type per file.
* **Interfaces:** `I` prefix (`IState`, `ISubSystem`, `IClientTransport`, `IDataProxy`).
* **Abstract bases:** `Base` suffix (`SubSystemBase`, `EnumStateBase`). Keep generic parameters on generic bases.
* **Managers:** `Manager` suffix (`GameStateManager`, `DataProxyManager`).
* **Static utilities:** `Utils` suffix, `static class` (`MathUtils`, `NetUtils`).
* **Concrete states:** `State` suffix (`EntityIdleState`, `LoadingState`).
* **Network names:** keep `NetworkObjectIdentity`, `CharacterPresentationAdapter`, `NetClient`, `NetServer`.

## Fields and Properties

```csharp
// ❌ BAD

private int ClientId;

[SerializeField] private int _entityId;

// ✅ GOOD

private int _clientId;

[SerializeField] private int entityId;
```

* **Private fields:** `_camelCase` (including private static: `_config`, `_instance`, `_fsm`).
* **`[SerializeField] private`:** `camelCase`, no underscore (`animIn`, `entityId`).
* **Public fields:** `camelCase`; prefer properties when there is logic (`public bool tickDrive;`).
* **`protected readonly`:** `PascalCase` (`protected readonly EntityContext Context;`).
* **Private `static readonly` collections/locks:** `PascalCase` (`Lock`, `SubSystems`).
* **Properties:** `PascalCase` (`Instance`, `EntityId`, `IsRunning`); expression-bodied getters for simple accessors.

## Methods

* **Public / `protected virtual`:** `PascalCase` (`Move`, `Init`, `Tick`, `CheckStateChange`).
* **Unity lifecycle:** keep Unity names (`Awake`, `Update`, `OnDestroy`, …).
* **Framework hooks:** only `_Init()` and `_Destroy()`; do not add other public underscore APIs.
* **Async:** `Async` suffix (`LoadResourceAsync`).
* **Parameter wrap:** wrap at line width, several parameters per line, continuation indent 4 spaces. Do not put one parameter per line.

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
7. Lifecycle — `#region 生命周期` (MonoBehaviour) or `#region 子系统生命周期` (`SubSystemBase`)
8. Net Handler Last — `#region 网络消息处理`

Reference: `NetworkObjectIdentity.cs`. Preserve legacy feature regions in `EntityCharacter.cs`; do not copy them into new files.

## File Organization

* One primary type per file; file name matches the type.
* New code may use only `#region 属性`, `#region 事件`, `#region 生命周期`, `#region 子系统生命周期`.
* XML summaries on public types, public methods, and non-obvious `protected virtual` methods when useful.
* Use `[Header]`, `[Tooltip]`, `[SerializeField]`, `[RequireComponent]`, `[DisallowMultipleComponent]` where they fit.
* Match the surrounding file for `var` vs explicit types and expression-bodied members.

## Data Flow, Not Defensive Code

Trace **producer → owner/store → consumer** before writing code.

Prefer explicit data flow and established lifecycle contracts over defensive recovery. Assume required dependencies, state, and data are valid when the surrounding architecture guarantees that they have already been established.

Do not add defensive branches merely because a value **could theoretically** be missing, invalid, or uninitialized.

When a required invariant is broken, do not silently recover, skip work, substitute a default, or redirect control flow just to keep execution going. Fix the producer, owner, initialization order, registration, or lifecycle contract instead.

### Forbidden

Do not introduce helpers or branches whose purpose is to hide a broken invariant.

* `EnsureXxx`
* `GuardXxx`
* `GetOrCreateXxx` when missing state indicates incorrect setup
* `TryGet` / `TryFind` used only to avoid accessing a required dependency
* Null checks that only convert a broken dependency into an early `return`
* Empty checks that only suppress required work
* Default values that hide missing or invalid producer output
* Fallback control flow that conceals incorrect initialization or lifecycle ordering
* Repeated defensive checks on every consumer instead of enforcing the invariant at its owner

```csharp
// ❌ BAD — hides missing initialization

private EntityCharacter EnsureCharacter()
{
    if (_character == null)
        _character = new EntityCharacter();

    return _character;
}
```

```csharp
// ❌ BAD — hides a broken dependency contract

public void Tick()
{
    if (_simulation == null || _registry == null)
        return;

    _simulation.Step(_registry);
}
```

### Expected Runtime Conditions

Defensive handling is appropriate when absence or failure is a **normal and explicitly supported result** of the operation.

Examples include:

* Optional dependencies
* User input that may legitimately be absent
* External or untrusted data
* Network data that may legitimately be delayed, duplicated, or invalid
* Queries whose contract explicitly allows "not found"
* Operations whose API explicitly defines failure as an expected result

Do not use defensive handling merely because an API permits failure if the surrounding code has already established that the operation must succeed.

### `TryXxx`

Use `TryXxx` when failure is part of the operation's normal contract.

Do not use `TryXxx` as a general-purpose replacement for accessing a required dependency.

The existence of a possible failure path in an API does not by itself mean that the caller should silently handle that failure.

### Ownership of Invariants

Enforce invariants at the point where the relevant data or state is created, registered, initialized, or otherwise established.

Consumers should rely on established invariants rather than repeatedly validating them.

If multiple consumers require the same condition, prefer strengthening the producer/owner contract over adding the same defensive check to every consumer.

### Early Returns

An early return is not inherently defensive code.

Use an early return when it represents an intentional and valid control-flow condition.

Do not use an early return to silently terminate required work because an invariant was unexpectedly violated.

### Review Rule

Before adding a null check, `TryXxx`, fallback, default value, or early return, ask:

> **Is this an expected runtime condition, or am I hiding a broken invariant?**

If it is an expected runtime condition, handle it explicitly.

If it is a broken invariant, do not add defensive recovery. Trace the **producer → owner → consumer** chain and fix the source of the invalid state.
