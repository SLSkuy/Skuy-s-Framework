---
name: "design"
description: "Turn confirmed exploration context into implementation-ready design and task documents under Docs/Plan without changing project code. Use after exploration or when the user has already chosen a technical direction."
---

# Design

Create implementation-ready planning artifacts from confirmed exploration context. This is the bridge from `explore` to implementation: capture why the change is needed, the selected technical approach, the exact impact surface, and an ordered task checklist.

This skill is inspired by OpenSpec's proposal artifact flow, but it uses this repository's native `Docs/Plan/` structure and requires no OpenSpec CLI.

## Planning Boundary

- Create or revise planning documents only.
- Do not modify source code, assets, scenes, configuration, `.asmdef`, protocols, generated files, or tests.
- Do not begin implementation after writing the documents.
- Write documents in English with CRLF line endings.
- Follow `AGENTS.md`, `.agents/rules/Architecture.md`, and `.agents/rules/Documentation.md`.

## Required Input

Use the current conversation's confirmed exploration result when available. It should establish the problem, assumptions, repository evidence, selected approach, rejected alternatives, risks, and unresolved questions.

Before writing, re-read the relevant repository files, active plan documents, and related archived plans. Archived plans are historical evidence only; verify their facts against current code. Conversation context may be stale or incomplete. If a missing decision would materially change module boundaries, public interfaces, protocol/serialization, assembly dependencies, lifecycle order, or task breakdown, ask the user and stop. Otherwise proceed with explicit documented assumptions.

If exploration has not occurred but the user has already provided a clear problem and chosen direction, gather the minimum repository evidence needed and continue. If the direction itself is unclear, recommend `explore` instead of inventing a design.

## Change Identity

Derive a concise English `PascalCase` feature folder name and a matching `kebab-case` file stem. Use:

```text
Docs/Plan/<Feature>/<feature>-design.md
Docs/Plan/<Feature>/<feature>-tasks.md
```

Example:

```text
Docs/Plan/CharacterReplication/
├── character-replication-design.md
└── character-replication-tasks.md
```

When the same goal and scope already have a plan folder, update it rather than creating a duplicate. Create a new folder when the problem or intended outcome has fundamentally changed. Never overwrite user-authored content blindly; read and reconcile it.

## Artifact Order

Create artifacts in dependency order: design first, then tasks. Re-read the completed design from disk before producing tasks.

Also create or update the plan's row in `Docs/Plan/README.md`. The row must link both artifacts and mirror the design's workflow status. Do not create an index file inside the feature folder.

### Design Document

The design must be self-contained and implementation-ready. Include only relevant sections, but cover these facts:

```markdown
# <Feature Name>

## Workflow Status
Plan Status: <Draft | Awaiting Confirmation | Approved>
Confirmation Gate: <Not Triggered | Awaiting Confirmation | Confirmed>
Confirmation Evidence: <None, or a concise user-confirmation reference and date>

## Background and Goal
## Scope
## Assumptions and Decisions
## Repository Evidence
## Data Sources and Ownership
## Selected Design
## Modules and Files Affected
## Interface and Call-Chain Changes
## Lifecycle and Dependency Impact
## Expected Behavior
## Alternatives Rejected
## Risks and Open Questions
## Verification
```

Requirements:

- Explain why the change is needed and what observable result defines success.
- Separate in-scope work from explicitly out-of-scope work.
- Cite repository evidence with paths, types, methods, or call relationships.
- Describe the selected mechanism and data/control flow. Use a compact ASCII diagram when it materially clarifies the design.
- List files expected to be added, modified, moved, or deleted, grouped by module. Mark uncertain paths as provisional.
- State public API, call-site, `.asmdef`, protocol/serialization, lifecycle, and compatibility impact explicitly, including "none" where verified.
- Incorporate external references from exploration by link and explain what is adapted versus rejected; do not repeat a full research report.
- Record rejected alternatives and the project-specific reason they were rejected.
- Define verification that can demonstrate the intended behavior.
- Record the workflow state and `AGENTS.md` confirmation gate using the exact fields above.
- Use `Awaiting Confirmation` when the gate is triggered without durable confirmation. Use `Confirmed` only when the user explicitly approved the stated migration boundaries and benefits; record that evidence without inventing approval.

Do not include implementation code. Short signatures, data shapes, pseudocode, and diagrams are allowed only when necessary to make a contract unambiguous.

### Task Document

Generate an ordered, executable checklist derived from the final design:

```markdown
# <Feature Name> Tasks

## Preconditions
- [ ] P1. ...

## Implementation
- [ ] I1. ...

## Verification
- [ ] V1. ...

## Documentation
- [ ] D1. ...
```

Task requirements:

- Each checkbox represents one reviewable outcome, not a vague activity.
- Give every checkbox a stable section-prefixed identifier so later sessions can reference it even if wording changes.
- Order tasks by dependency and lifecycle sequence.
- Name the owning module and expected files/types when known.
- Include required removals and call-site migrations; do not preserve superseded code unless compatibility is explicitly required.
- Include focused tests or Unity Console/manual checks appropriate to the risk.
- Include architecture-document synchronization only when module facts change.
- Add confirmation-gate decisions as preconditions before affected implementation tasks.
- Do not mark implementation tasks complete while creating the plan.
- Avoid speculative future work. Put deferred scope in the design, not as current tasks.

## Consistency Review

Before reporting completion:

1. Re-read both files from disk.
2. Confirm every design change has at least one task and every task is justified by the design.
3. Confirm module ownership, dependency direction, lifecycle, interfaces, data sources, removals, verification, and documentation impact agree across both files.
4. Confirm `Docs/Plan/README.md` links both artifacts and mirrors the workflow status.
5. Check links and paths, English-only prose, direct non-numeric file names, CRLF line endings, and `git diff --check`.
6. Report the created/updated paths, selected approach, confirmation-gate status, unresolved decisions, and that no implementation code was changed.

The artifacts remain a live plan. If the user later changes the approach but keeps the same goal, revise both documents and the root plan-index status together so they stay coherent. When the plan is approved and implementation is requested, explicitly hand off to `apply` with both artifact paths.
