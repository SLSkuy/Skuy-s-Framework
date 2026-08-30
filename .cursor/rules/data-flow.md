---
description: Ban EnsureXxx-style guards; follow data flow, do not over-protect
globs: Assets/Scripts/**/*.cs
alwaysApply: false
---

# Data Flow, Not Defensive Code

Trace producer → store → consumer before writing. Put invariants on the owner of the data. Do not hide broken wiring with protective helpers.

## Forbidden

- `EnsureXxx` / `GuardXxx` / `GetOrCreateXxx` that silently create or skip when state is missing
- Null / empty checks on every hop that swallow a missing setup and `return`
- Defaulting absent inputs so the caller never sees the bug

```csharp
// ❌ BAD — hides that Init never ran
private EntityCharacter EnsureCharacter()
{
    if (_character == null)
        _character = new EntityCharacter();
    return _character;
}

// ❌ BAD — consumer papers over a missing producer
public void Tick()
{
    if (_simulation == null || _registry == null) return;
    _simulation.Step(_registry);
}
```

## Allowed

- `TryGet` / `TryGetValue` when absence is a real domain outcome (lookup miss, optional component)
- Buffer grow/resize for I/O (name it `Grow` / `Resize`, not `Ensure`)
- A single public-entry `ArgumentNullException` only when the method is an external contract and the caller is outside this assembly

