---
name: "apply"
description: "Implement an approved Docs/Plan design by working through its task checklist in order, verifying each outcome and updating task checkboxes. Use when design and task documents already exist and the user wants implementation to begin or resume."
---

# Apply

Implement an approved plan from `Docs/Plan/<Feature>/` one task at a time. Treat the design as the implementation contract and the task document as resumable progress state.

This skill follows OpenSpec apply's task-driven behavior but uses this repository's native design/task documents and requires no OpenSpec CLI.

## Required Inputs

Identify one plan containing both:

```text
Docs/Plan/<Feature>/<feature>-design.md
Docs/Plan/<Feature>/<feature>-tasks.md
```

Use an explicit feature/path when provided. Otherwise infer it only when the conversation or repository has one unambiguous active plan. If multiple plausible plans contain unchecked tasks, ask the user to select one.

Do not apply a plan when:

- the design or task document is missing;
- a design decision that changes implementation is unresolved;
- the design and tasks materially disagree;
- an `AGENTS.md` confirmation gate is triggered but not confirmed.

In those cases, report the conflict and stop. Use `design` to revise planning artifacts when the approach must change.

## Establish the Baseline

Before editing implementation files:

1. Read `AGENTS.md`, the design, the full task document, relevant architecture documents, and directly affected code/tests.
2. Re-read these files from disk even if they appeared earlier in the conversation; the user may have edited them.
3. Inspect `git status` and relevant diffs. Treat pre-existing changes as user work: preserve them, build on them when necessary, and never revert unrelated changes.
4. Confirm the selected design still matches repository reality. Minor path/name drift may be resolved from code evidence and noted; architectural divergence requires a planning revision.
5. Find the first unchecked task whose dependencies are complete.

Read the exact `Plan Status`, `Confirmation Gate`, and `Confirmation Evidence` fields from the design. Before changing implementation, the plan must be `Approved` or `In Progress`; a triggered gate requires `Confirmed` with non-empty evidence. When the user's current request supplies a missing explicit confirmation, record it in the design and task precondition before continuing. Never infer approval from existing code changes.

If every task is already checked, audit completion instead of making new changes.

## Task Loop

Work through tasks sequentially unless the user explicitly requests one task only or a pause after each task.

For each task:

1. **Announce:** state the exact checkbox being implemented and the expected files/outcome.
2. **Inspect:** search for existing abstractions and read affected code/call sites before editing.
3. **Implement:** make the smallest change that satisfies the task and design. Follow module boundaries, lifecycle, naming, member order, CRLF, and comment/dead-code rules.
4. **Verify:** run the narrowest meaningful check for that outcome. Use compilation/static checks, focused tests, Unity Console, or scene/manual checks according to task risk and available tools.
5. **Synchronize artifacts:** update factual paths, types, interfaces, call chains, behavior, or verification in the design and affected tasks before recording progress. This keeps the plan descriptive of the implemented result; do not add a change log.
6. **Update progress:** change `- [ ]` to `- [x]` only after the task's observable outcome is complete and its required verification passes. Preserve the task identifier and wording unless repository facts require a small clarification.
7. **Report progress:** briefly state what completed and which task comes next, then continue.

Re-read the task document after external/user edits or when resuming a later session. Never mark several tasks complete only at the end; progress must remain recoverable after each task.

When the first implementation task starts, set the design and `Docs/Plan/README.md` status to `In Progress`. If work stops on a durable blocker, set both to `Blocked`; restore `In Progress` when the blocker is resolved.

## Implementation Rules

- The design determines **how**; tasks determine **execution order**. Do not silently substitute a different architecture.
- Reuse existing abstractions and search before adding types.
- Delete superseded code when the design replaces it. Preserve compatibility only when the design explicitly requires it.
- Do not modify generated protocol files or third-party files manually. If a task changes a protocol, edit its source definition and use the established generator workflow; stop if that workflow is unavailable or unauthorized.
- Do not add lazy initialization or `EnsureXXX()` self-healing to hide lifecycle errors.
- Do not broaden scope to speculative cleanup or future work.
- Do not create commits, branches, pull requests, or external side effects unless the user explicitly asks.

## Plan Drift and Blockers

Small implementation discoveries may be reflected by updating the relevant design facts and adding or refining a task when they do not alter intent, module boundaries, public contracts, protocol/serialization, assembly dependencies, or lifecycle order.

Stop and ask the user when:

- the selected approach is no longer viable;
- required work crosses the design's declared scope;
- a public API, protocol, `.asmdef`, lifecycle, or compatibility decision changes;
- the next task depends on missing information or unavailable external state;
- verification exposes a design-level flaw rather than a local implementation bug.

Keep the current task unchecked when blocked. Record the exact blocker in the report; do not encode an unapproved architectural decision into code or tasks.

## Verification Discipline

- Documentation-only task: validate paths, links, English prose, and CRLF.
- C# or configuration task: run available compilation/static checks and focused tests.
- Unity object, prefab, scene, or lifecycle task: use `unity-mcp-orchestrator` when available; wait for compilation and check the Unity Console.
- Behavioral/network task: run the plan's focused EditMode/PlayMode or manual scenario checks when available.

If a required verification cannot run, leave the associated verification task unchecked and report what remains. Do not claim the plan is complete based only on code inspection.

## Completion Audit

When no unchecked tasks remain:

1. Re-read the design and task documents.
2. Confirm implementation matches the selected approach, scope, interfaces, call chain, dependency direction, lifecycle, removals, and expected behavior.
3. Run the plan's final verification and `git diff --check`.
4. Confirm required `Docs/Architecture/` synchronization is complete.
5. Confirm the design describes the final implemented paths, types, interfaces, call chain, behavior, and verification result.
6. Set the design and `Docs/Plan/README.md` status to `Complete` only after all required checks pass.
7. Report changed files, completed tasks, verification results, residual risks, and any manual checks left for the user.

Do not archive the plan during implementation. After the plan reaches `Complete`, report that the user may invoke `archive`; only that explicit post-completion workflow moves it out of `Docs/Plan/`.
