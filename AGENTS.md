## Agent skills

### Issue tracker

Issues live as markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical role labels: needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one CONTEXT.md and docs/adr/ at the repo root. See `docs/agents/domain.md`.

### Cursor rules

Persistent project rules live in `.cursor/rules/` (apply when editing `Assets/Scripts/**/*.cs`):

- `architecture.md` — project structure, namespaces, composition/lifetimes, patterns
- `coding-style.md` — C# naming, member order, regions, and data-flow (no `EnsureXxx` / silent guards)
