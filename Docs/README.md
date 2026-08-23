# Documentation Index

This directory contains module and feature design documents. Normative rules live in [`.agents/rules/`](../.agents/rules/); `Docs/` contains only module and feature documentation.

## Layout

```text
Docs/
├── README.md
├── Plan/                 # Feature designs and implementation plans
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

Feature designs and implementation plans live under `Docs/Plan/<Feature>/`. Documents use direct feature/purpose names without numeric prefixes. Module facts remain under `Docs/Architecture/`. See [`.agents/rules/Documentation.md`](../.agents/rules/Documentation.md) for layout and document levels.

## Note

Module lists, namespaces, and type names drift during refactoring. These documents describe the architecture at writing time; always verify against the actual code before acting. See [AGENTS.md](../AGENTS.md) and its Repository Truth rule.
