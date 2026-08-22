# Docs 索引

本目录存放按功能/模块组织的设计文档。规范性文件（规则）位于 [`.agents/rules/`](../.agents/rules/)，本目录只放**模块设计文档**与**功能设计文档**。

## 目录结构

```
Docs/
├── README.md            # 本索引
└── Architecture/        # 模块架构文档（按模块划分）
    ├── framework.md     # Framework 核心框架
    ├── entity.md        # EntitySystem 实体系统
    ├── simulation.md    # EntitySimulationCore + NetworkSync 模拟与网络同步
    └── networking.md    # Network 网络层
```

## 模块架构文档

| 模块 | 文档 |
| --- | --- |
| Framework 核心框架 | [Architecture/framework.md](Architecture/framework.md) |
| EntitySystem 实体系统 | [Architecture/entity.md](Architecture/entity.md) |
| 模拟核心与网络同步 | [Architecture/simulation.md](Architecture/simulation.md) |
| Network 网络层 | [Architecture/networking.md](Architecture/networking.md) |

## 功能设计文档

按功能划分的设计文档放在以功能名命名的子文件夹下（如 `Docs/EntityControl/`、`Docs/Navigation/`），每个文件夹配一个 `00-Index.md`。布局与自包含要求见 [.agents/rules/Documentation.md](../.agents/rules/Documentation.md)。

## 注意

模块清单、命名空间、类名会随重构漂移。文档描述的是**写作时**的架构，行动前始终以实际代码为准 —— 见 [AGENTS.md](../AGENTS.md) 的「仓库即事实」。
