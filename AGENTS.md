# Repository Guide

This is a Unity project. This file is the **Agent behavior constitution**: it defines how an Agent works and what it must or must not do. Project details (architecture, namespaces, coding style, and documentation) live in `[.agents/rules/](.agents/rules/)`. **Do not make this file a second source of truth** for module lists, namespaces, or type names; those drift during refactoring, so use the actual code as the authority.

## 1. Agent Workflow

Every task follows this cycle.

### Clarify

When the goal, scope, or terminology is ambiguous, ask the user or state explicit assumptions before continuing. Include those assumptions in the planning explanation; do not complete a full cycle in the wrong direction.

### Understand

Before changing code, read the relevant files, locate the owning module, find similar implementations, and confirm dependency direction and lifecycle. When uncertain, search the code first; do not invent abstractions.

### Plan

State which files will change, which types will be added, affected interfaces/call sites, cross-module dependencies, and whether documentation or tests must be updated.

**Confirmation gate:** Before implementation, explain migration boundaries and benefits and obtain confirmation when any of these apply: changing public interfaces in two or more runtime modules; changing `.asmdef` files or assembly dependencies; changing protocols or serialization formats; changing lifecycle order; or moving existing types and migrating call sites. Editing implementation details across multiple directories alone does not trigger the gate; see **Scope Control**.

### Implement

Follow the owning module's existing architecture. Reuse existing interfaces and components. Do not change unrelated code, keep replaced implementations, or modify generated/third-party files.

### Verify

Run the minimum verification appropriate to the risk: check paths and links for documentation changes; run available compilation, static checks, or Unity Console checks for C# or configuration changes; run related tests when behavior changes and tests exist. If Unity or another verification condition is unavailable, report the unverified risk explicitly.

### Report

Summarize what changed, why, what was verified, and any unverified risk. For architecture or externally visible behavior changes, state whether `Docs/Architecture/` is synchronized and what remains for the user.

## 2. Change Principles

1. Reuse existing abstractions; search before creating new capabilities.
2. Do not break module boundaries for local requirements.

Do not refactor the existing architecture merely to follow generic best practices. The repository and its conventions take priority; see **Repository Truth**.

## 3. Architecture Rules

- **Module boundaries:** identify the owning module before editing and do not add responsibilities across boundaries. See `[.agents/rules/Architecture.md](.agents/rules/Architecture.md)`.
- **Dependency direction:** keep assembly dependencies explicit. Pure simulation/core assemblies must remain independent from Unity scene glue, gameplay hosts, transport, and test assemblies. Do not add `.asmdef` files before recording the boundary and direction.
- **Scene glue vs. core:** MonoBehaviours such as `EntityCharacter`, `UIController`, and `NetworkObjectIdentity` bridge Unity lifecycle to framework services. Reusable logic belongs in `SubSystemBase` or a pure core assembly, not in scene glue.
- **New-code migration:** once a module has an approved target folder/assembly, put new code there. Do not extend legacy/compatibility files with new behavior. Compatibility shims must be temporary, documented, and tracked by a removal task.
- **Lifecycle:** framework lifecycle is driven by `SubSystemBase._Init()` / `Init()` / `Destroy()`. Do not use `EnsureXXX()`, lazy initialization, or automatic self-healing to hide lifecycle errors; missing initialization should fail loudly.



## 4. Coding Rules

Only high-level points are listed here; see `[.agents/rules/CodingStyle.md](.agents/rules/CodingStyle.md)` for details.

- **Namespaces:** only `Launch` and `MainEntry` may use the global namespace. All other types must declare a module namespace (see `Architecture.md`).
- **Naming:** types/methods/properties use `PascalCase`; private fields use `_camelCase`; `[SerializeField]` fields use `camelCase` without an underscore; interfaces use the `I` prefix; abstract bases use the `Base` suffix; managers, utilities, and states use `Manager`, `Utils`, and `State` suffixes.
- **Member order:** serialized fields -> class references -> primitive values -> `#region Properties` -> `#region Events` -> methods -> lifecycle regions at the end.
- **Regions:** use only `Properties`, `Events`, `Lifecycle`, and `Subsystem Lifecycle` regions. Do not region ordinary methods.



## 5. Change Constraints



### Forbidden

- Do not modify generated protocol files under `GamePlay/Protocol/Generated/` or third-party packages unless explicitly requested.
- Do not perform unrelated refactors.
- Do not add `EnsureXXX()` lazy self-healing guards.
- Do not introduce speculative abstractions because they appear more "best practice" compliant.
- Do not create an abstraction before searching for an existing one.



### Comments and Dead Code

- Preserve existing comments and update them in place; keep banner comments attached to their field groups.
- Delete replaced implementations. Do not leave them as comments, `#if false`, or unused code. Keep compatibility code only when explicitly requested and label it `// Legacy implementation retained for compatibility`.



### Scope Control

Prefer the smallest necessary change. Do not refactor unrelated code for a local requirement. Cross-module refactoring is justified only when the current architecture cannot satisfy the requirement, an existing abstraction clearly blocks correctness, the task explicitly requests it, or migration boundaries and benefits are clear. Use the confirmation gate before implementation when triggered.

### Repository Truth

For existing architecture, the repository is the source of truth. Do not guess from generic Unity/ECS/network experience. Search code when responsibilities, lifecycle, or call relationships are unclear. Do not replace the current design merely because another design is more conventional.

## 6. Documentation

When a change adds a subsystem, crosses module boundaries, adds a protocol, or records an architectural decision, create or update a document under `Docs/Plan/<Feature>/`. Use the minimal template for ordinary features and the full template for architecture, protocol, and cross-module work. See `[.agents/rules/Documentation.md](.agents/rules/Documentation.md)`.

**Architecture synchronization:** when adding a top-level module directory or an independent `.asmdef` module, create its module document under `Docs/Architecture/` (responsibility, structure, core abstractions, dependency direction) and update [Docs/README.md](Docs/README.md). For substantial changes to an existing module's responsibility, core abstractions, or dependency direction, update its architecture document.

**Plan synchronization:** design and task documents are live implementation records. During implementation, keep task progress, design facts, confirmation status, and the matching entry in `Docs/Plan/README.md` synchronized. Update factual implementation drift in place; return to planning before changing the approved technical direction. Completed plans remain active until the user explicitly requests archival; archive the complete feature folder under `Docs/Archive/` and update both indexes.

## 7. Rule Index


| Topic                                                | File                                                               |
| ---------------------------------------------------- | ------------------------------------------------------------------ |
| Architecture, modules, namespaces, patterns          | `[.agents/rules/Architecture.md](.agents/rules/Architecture.md)`   |
| C# style, member order, regions                      | `[.agents/rules/CodingStyle.md](.agents/rules/CodingStyle.md)`     |
| Documentation layout and self-contained requirements | `[.agents/rules/Documentation.md](.agents/rules/Documentation.md)` |


Active feature design documents live under `Docs/Plan/`, archived plans under `Docs/Archive/`, and module architecture documents under `Docs/Architecture/` (see [Docs/README.md](Docs/README.md)). Plan documents use direct feature/purpose names without numeric prefixes.