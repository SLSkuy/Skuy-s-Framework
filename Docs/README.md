# Documentation Index

This directory contains module and feature design documents. Normative rules live in [`.agents/rules/`](../.agents/rules/); `Docs/` contains only module and feature documentation.

## Layout

```text
Docs/
├── README.md
├── Plan/                 # Feature designs and implementation plans
├── Archive/              # Completed historical plans
└── Architecture/        # Module architecture documents
    ├── framework.md
    ├── entity.md
    ├── simulation.md
    └── networking.md
```

## Module Architecture

| Module | Document |
| --- | --- |
| Framework core | [Architecture/framework.md](Architecture/framework.md) |
| EntitySystem | [Architecture/entity.md](Architecture/entity.md) |
| Simulation core and network synchronization | [Architecture/simulation.md](Architecture/simulation.md) |
| Network layer | [Architecture/networking.md](Architecture/networking.md) |

## Feature Plans

Feature designs and implementation plans live under `Docs/Plan/<Feature>/`. Documents use direct feature/purpose names without numeric prefixes. See the [feature plan index](Plan/README.md) for active plans and workflow status. Module facts remain under `Docs/Architecture/`. See [`.agents/rules/Documentation.md`](../.agents/rules/Documentation.md) for layout and document levels.

## Archived Plans

Completed plans explicitly archived by the user live under `Docs/Archive/<YYYY-MM-DD>-<Feature>/`. They remain searchable historical evidence but are not active `apply` inputs. See the [archive index](Archive/README.md).

## Note

Module lists, namespaces, and type names drift during refactoring. These documents describe the architecture at writing time; always verify against the actual code before acting. See [AGENTS.md](../AGENTS.md) and its Repository Truth rule.
