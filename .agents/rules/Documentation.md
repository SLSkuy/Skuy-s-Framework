# Documentation

Design documents live under the root `Docs/` directory, outside `Assets/`. Workflow rules are defined in [AGENTS.md](../../AGENTS.md).

## Layout

Plan documents are grouped by feature under `Docs/Plan/<Feature>/`. Module facts belong under `Docs/Architecture/`:

- **One feature per folder:** use a descriptive name such as `Docs/Plan/EntityControl/` or `Docs/Plan/Navigation/`; keep that feature's plan documents together.
- **Direct names:** name plan documents after the feature or purpose, such as `movement-design.md` or `network-sync-plan.md`; do not use numeric prefixes or `00-Index.md`.
- **File names:** use English `kebab-case` (ASCII and hyphens) for Git and cross-platform compatibility.
- **Language:** write new documentation in English. Keep code symbols, type names, identifiers, and paths unchanged.

## Document Levels

An ordinary feature document must state at least: goal, affected files, expected behavior, and verification method.

Architecture, protocol, and cross-module documents must be self-contained and understandable without conversation history. They must additionally state:

1. **Data source:** where data is read from, such as a config path, `ScriptableObject` directory, protobuf message, or subsystem obtained through `Global.Get<T>()`.
2. **Modules involved:** current namespace/folder/types and the exact destination for new code.
3. **Required changes:** files, interfaces, call sites, and expected behavior after the change.
4. **Interface/call-chain changes:** what breaks and what must be updated elsewhere.
5. **Expected result:** observable behavior after the change.

Do not write vague documents that only sketch an idea. Ordinary feature documents may be short; full documents must make data sources, module ownership, and changes explicit so a new conversation can implement them without guesswork.

## Minimal Template

```markdown
# <Feature Name>

## Goal

## Affected Files

## Expected Behavior

## Verification
```

## Full Template

Architecture, protocol, and cross-module documents extend the minimal template with the following sections:

```markdown
# <Feature/Architecture Name>

## Background and Goal

## Data Source

## Modules Involved

## Required Changes

## Interface/Call-Chain Changes

## Expected Behavior After Change

## Verification
```
