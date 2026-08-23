---
name: "archive"
description: "Move one explicitly selected, completed Docs/Plan feature into the dated Docs/Archive history and update plan indexes. Use only after apply has completed and the user asks to archive the plan."
---

# Archive

Archive one completed plan so active-plan discovery remains focused while the design and execution record stays available as historical evidence. This is an explicit post-completion action inspired by OpenSpec's validate-then-move archive flow; it requires no OpenSpec CLI.

## Boundaries

- Move documentation only. Do not modify implementation code, assets, scenes, protocols, generated files, tests, or architecture facts.
- Never archive automatically at the end of `apply`; require an explicit user request naming or unambiguously identifying one plan.
- Never delete the design or task record. Move the whole feature folder.
- Treat archived content as immutable history unless the user explicitly asks to correct it.

## Required Source

Select exactly one active folder containing:

```text
Docs/Plan/<Feature>/<feature>-design.md
Docs/Plan/<Feature>/<feature>-tasks.md
```

Do not infer a source when multiple complete plans are plausible. Archived plans under `Docs/Archive/` are not valid sources.

## Preflight

Re-read `AGENTS.md`, both plan artifacts, `Docs/Plan/README.md`, `Docs/Archive/README.md`, and relevant working-tree status. Archive only when all conditions hold:

1. `Plan Status` is exactly `Complete`.
2. `Confirmation Gate` is `Not Triggered` or `Confirmed`, with evidence when confirmation was required.
3. Every task checkbox is checked, including required verification and architecture-document synchronization.
4. The design describes the final implementation rather than an obsolete target.
5. No unresolved blocker or required manual verification remains.

If any condition fails, report the exact item and stop. Do not mark tasks complete, reinterpret missing verification, or change the plan to make it archivable.

## Destination

Use the current local date and preserve the feature name:

```text
Docs/Archive/<YYYY-MM-DD>-<Feature>/
```

Stop on a destination collision. Do not overwrite, merge, suffix, or delete an existing archive without explicit user direction.

## Archive Procedure

1. Search the repository for links to the active plan path and identify references that must follow the move.
2. Change the design's `Plan Status` to `Archived`.
3. Move the entire feature folder to the destination as one operation, preserving both artifacts and Git rename history where available.
4. Remove the plan's row from `Docs/Plan/README.md`.
5. Add one row to `Docs/Archive/README.md` with archive date, feature name, and links to the moved design and task documents.
6. Update repository documentation links that intentionally reference this plan. Do not rewrite historical prose or implementation facts.

## Verification

Before reporting completion:

1. Confirm the source folder no longer exists and the destination contains both expected artifacts.
2. Confirm the archived design says `Plan Status: Archived` and the task document contains no unchecked boxes.
3. Confirm the active index has no stale row and the archive index links resolve.
4. Check all changed relative Markdown links, English prose, CRLF line endings, and `git diff --check`.
5. Report the source, destination, index updates, verification results, and any unrelated working-tree changes left untouched.

The archive remains searchable through `Docs/Archive/README.md` and may inform future `explore` or `design` work, but it must never be resumed through `apply`.
