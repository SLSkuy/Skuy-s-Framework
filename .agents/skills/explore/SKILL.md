---
name: "explore"
description: "Explore an unclear feature, investigate the existing project, compare mature external implementations, and turn the discussion into a grounded direction without changing project code."
---

# Explore

Explore is a read-only thinking-partner mode for turning an unclear feature or technical problem into a grounded direction. Follow the conversation naturally, inspect the real repository, and compare alternatives. It is not an implementation phase or a rigid questionnaire.

## Use When

- The problem is known but the solution is not.
- Requirements, boundaries, inputs/outputs, or acceptance criteria are unclear.
- The relevant module, call chain, lifecycle, or dependency direction is unfamiliar.
- Multiple designs need trade-off analysis.
- The user asks for GitHub, official documentation, or mature implementation references.
- Version behavior of Unity, networking, algorithms, or third-party libraries must be verified.

Skip Explore when both the desired behavior and implementation path are already clear.

## Boundaries

- Do not create, edit, move, or delete project code, assets, scenes, configuration, generated files, or `.asmdef` files.
- Do not write exploration notes to `Docs/` or `Docs/Plan/`.
- Read files, search code, run read-only diagnostics, and browse public documentation/repositories as needed.
- Small snippets, pseudocode, and ASCII diagrams are allowed for discussion, but do not provide an implementation patch or instructions to paste one into a file.
- If the user asks to implement while exploring, explain the boundary and wait for direction before switching to the implementation workflow.

## Evidence-First Exploration

### 1. Frame the problem

Restate the problem and record current assumptions, inputs/outputs, acceptance criteria, constraints (Unity lifecycle, networking, performance, compatibility), and unknowns that could change the design. Continue with explicit assumptions when safe; do not mechanically block on every detail.

### 2. Index the repository

Read only relevant context, in roughly this order:

1. `AGENTS.md` and applicable `.agents/rules/`;
2. the matching `Docs/Architecture/` documents;
3. related `Docs/Plan/` documents;
4. directories, types, interfaces, call sites, tests, and configuration;
5. Unity state/resources only when Unity MCP is available and necessary.

Use `rg` / `rg --files`. Confirm module ownership, namespace, entry point, dependency direction, lifecycle, data flow, similar implementations, test entry points, and constraints. Every conclusion needs an evidence location (path, type, method, or call relationship).

Produce a compact **Repository Index** listing read documents, key files/types, entry call chains, related tests, and potentially relevant unread areas.

### 3. Decide whether to search externally

Search externally only when the repository lacks a useful analogue, the user requests it, or version behavior needs verification. If internal evidence is sufficient, say why no external search was needed.

### 4. Evaluate mature references

Use this source order: official docs/examples and maintainer repositories; active, directly relevant GitHub repositories; high-quality technical articles or papers; forums/issues only as leads.

For GitHub candidates, inspect recent commits/releases, issue/PR activity, license, supported Unity/language/network versions, and real runtime code. Check `Packages/manifest.json`, `.asmdef`, runtime, tests, and samples when present. Verify key APIs rather than relying on README claims. Compare at most three candidates in depth.

For each candidate, record:

```text
Source: <repository or official documentation URL>
Version: <release, commit, or docs version; state unknown explicitly>
License: <license; state unknown explicitly>
Evidence: <files, types, APIs, or sections inspected>
Problem solved: <what it actually addresses>
Reusable idea: <what fits this project>
Mismatch: <version, dependency, architecture, or lifecycle differences>
Decision: <adopt / adapt / reference only / reject>
```

### 5. Compare and converge

Present 1–3 candidates. For each, cover mechanism, project abstractions reused, new boundaries, interface/assembly/lifecycle impact, external-reference adaptations, complexity, performance, testability, migration cost, risks, and unknowns. Recommend one only when evidence supports it; otherwise state “do not recommend yet; verify X first.”

## Handoff Output

End with:

- confirmed problem and assumptions;
- Repository Index and internal evidence;
- external references and confidence/fit;
- options, recommendation, and rejected alternatives;
- open questions, risks, and required verification;
- whether `AGENTS.md`'s cross-module confirmation gate is triggered;
- next action: continue exploring, enter planning, or stop.

Exploration is not implementation approval. After the user confirms the direction, start the normal planning flow and create `Docs/Plan/<Feature>/` documentation only when required.

## Project Rules

- Follow [AGENTS.md](../../../AGENTS.md), especially repository truth, module boundaries, dependency direction, lifecycle, and the cross-module confirmation gate.
- Follow `.agents/rules/Architecture.md` and `.agents/rules/Documentation.md`.
- For Unity Editor work, follow `unity-mcp-orchestrator`'s resource-first, read-only discovery guidance.
