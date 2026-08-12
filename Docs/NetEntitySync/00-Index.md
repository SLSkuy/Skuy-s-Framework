# 网络对象 Transform 同步文档索引

## 文档目标

本目录定义实体系统向成熟网络同步架构演进时的设计基线。当前阶段把玩家、门、动态武器等对象统一视为网络对象，只处理其根节点在世界空间中的 `Position` 与 `Rotation` 同步，以及为保证这两项状态正确同步所必需的 Tick、输入命令、预测、权威校正、回放和远端插值能力。

本文档集描述架构和迁移要求，不替代代码实现。运行时代码允许按核心模拟、Gameplay、客户端/服务端复制和测试职责拆分为多个程序集；不改变现有 KCP/TCP 传输实现。

## 阅读顺序

1. [01-Transform-Sync-Architecture.md](01-Transform-Sync-Architecture.md)：网络对象模型、`EntityConfig` 配置、可组合基础同步组件、标识驱动激活、客户端/服务端双运行时、`EntityCharacter`/Controller 契约、技能能力扩展边界、协议设计、逐文件改造清单、`TestSimulator` Tick 驱动、阶段计划与验收标准。

## 当前范围

- 服务端权威模拟。
- 本地拥有实体的输入上传、客户端预测与权威校正。
- 非拥有实体的快照缓冲与位置、旋转插值。
- 单机模式复用同一套命令与模拟入口。
- 世界空间 `Position`、世界空间 `Rotation`、线速度、角速度和 `LastProcessedInputTick`。
- 一个网络对象可以组合多个基础能力组件，例如 `NetworkTransformCapability`、`NetworkAnimatorCapability`、`NetworkInputCapability`；网络对象标识负责统一激活、权限和生命周期，Gameplay 只引用所需能力，Controller 只写入意图。

## 暂不处理

- 缩放同步。
- 骨骼、Animator 参数和 Root Motion 网络同步。
- 生命值、技能、Buff、背包等 gameplay 状态。
- Lag Compensation、命中回溯和服务器历史世界。
- Interest Management、AOI 和按观察者裁剪。
- Delta Compression、位压缩和量化编码。
- 客户端权威 Transform。
- 载具、父子网络实体和相对空间 Transform。
- DOTS / Netcode for Entities 迁移。

## 当前验证夹具

`Assets/Scripts/GamePlay/NetworkSync/TestSimulator/` 中的 `MultiPlayManager`、`ServerSimulator`、`ClientSimulator` 被定义为客户端和服务端的 Tick 驱动器，同时承担同步闭环验收夹具职责。它们不拥有网络对象同步算法，必须驱动框架基础组件和统一 Controller/Simulation 管线。纯模拟核心位于 `EntitySimulationCore/`，Unity Gameplay 位于 `EntitySystem/`，网络复制位于独立的 `NetworkSync/`；后续新代码必须进入对应职责目录。

## 文档状态

- 状态：阶段 0、阶段 1、阶段 2 已完成；阶段 3 的远端插值与阶段 4 的客户端预测待继续实现。
- 基于项目版本：Unity `6000.5.6f1`，提交 `7e1430a`。
- 最后更新：2026-08-13。
