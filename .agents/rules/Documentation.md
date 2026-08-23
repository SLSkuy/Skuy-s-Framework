# Documentation

Design documents live under the root `Docs/` directory, outside `Assets/`. Workflow rules are defined in [AGENTS.md](../../AGENTS.md).

## Layout

Active plan documents are grouped by feature under `Docs/Plan/<Feature>/`. Completed historical plans belong under `Docs/Archive/<YYYY-MM-DD>-<Feature>/`. Module facts belong under `Docs/Architecture/`:

- **One feature per folder:** use a descriptive name such as `Docs/Plan/EntityControl/` or `Docs/Plan/Navigation/`; keep that feature's plan documents together.
- **Direct names:** name plan documents after the feature or purpose, such as `movement-design.md` or `network-sync-plan.md`.
- **File names:** use English `kebab-case` (ASCII and hyphens) for Git and cross-platform compatibility.
- **Language:** write new documentation in English. Keep code symbols, type names, identifiers, and paths unchanged.
- **Plan index:** `Docs/Plan/README.md` lists each plan, links its design and task documents, and records its current workflow status. Do not create a second index inside feature folders.
- **Archive index:** `Docs/Archive/README.md` lists archived plans. Archive folders use a completion-date prefix for uniqueness, while document filenames retain their direct non-numeric names.



## Workflow State

Implementation-ready plans use one of these states: `Draft`, `Awaiting Confirmation`, `Approved`, `In Progress`, `Blocked`, `Complete`, or `Archived`. The design document is the source of truth for the state and confirmation-gate decision; the active or archive index mirrors it for discovery.

When implementation reveals factual changes to paths, types, interfaces, call chains, or verification, update the design and tasks together. A changed technical direction requires returning to planning and obtaining confirmation again when the `AGENTS.md` gate applies. This is live plan maintenance, not a change log.

Archival is an explicit post-completion action. Move the complete feature folder rather than copying it, remove its active-plan index row, and add an archive-index row. Archived plans are historical evidence for future exploration, not valid input for `apply`.

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
