# 变更记录

本项目所有变更记录遵循 [Keep a Changelog](https://keepachangelog.com/) 风格，版本号遵循 [Semantic Versioning](https://semver.org/)。

影响项目行为、结构、工作流、工程原则、指令文件或关键配置的变更，必须记录在 `[Unreleased]` 下。

## [Unreleased]

### 变更
- 审查并修订 `AGENTS.md` Agent 工作流：新增「澄清」步骤与跨模块重构「确认关卡」；删除「验证（Verify）」步骤（编译与测试验证由用户在 Unity 中执行）；「注释/死代码」规则从编码规则归位到修改约束；压缩「修改原则」消除与工作流的重复；修复指向「修改约束」的失效交叉引用；索引处回补 Git 提交约束一句话。
- 重构项目指令文档结构：`AGENTS.md` 精简为 Agent 行为宪法（约 100 行），详细规范拆分到 `.agents/rules/`（Architecture / CodingStyle / Testing / Documentation / Git）。
- 新增模块架构文档目录 `Docs/Architecture/`，含 `framework.md`、`entity.md`、`simulation.md`、`networking.md` 四篇自包含模块文档。
- 新增 `Docs/README.md` 作为 Docs 索引。
- 全部指令与文档文件统一为中文，行尾统一为 CRLF。

### 新增
- `CHANGELOG.md` 变更记录。

## 版本说明

- `Added` 新增功能
- `Changed` 对既有功能的变更
- `Deprecated` 即将移除
- `Removed` 已移除
- `Fixed` 缺陷修复
- `Security` 安全相关
