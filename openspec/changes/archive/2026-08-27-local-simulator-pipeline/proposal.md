## Why

当前单机与联机角色推进都挤在 `CharacterReplicationSystem` 里：本机输入采样、`EntityCharacter.Step`、Tick 时钟和网络复制绑在同一条路径上。Simulator 大模块已有空壳（Tick、Identity、Config），但尚未形成可独立运行的模拟链路。需要先打通**不依赖 MultiPlay 的单机路径**，把后续客户端/服务端 Host 都要复用的生成、命令捕获和步进变成稳定内核，再接入房间、消息和预测。

## What Changes

- 落地 `GamePlay.Simulator` 模拟核：唯一 Tick、实体注册表、附身、按 tick 命令邮箱、按 Role 调度 `EntityCharacter.Step`。
- 落地 **LocalHost** 装配：生成 LocalPlay 角色、从 `LocalInputProvider` 捕获本拍 `InputState`、写入邮箱并步进；不挂预测、插值或网络传输。
- 抽出可复用的 **Spawn / Command** 子模块到 `GamePlay.Simulator`：生成逻辑从 `PlayerSpawner` 迁入；命令邮箱与 `EntityCommandBuilder` 迁到 Simulator 命令目录。联机复制若仍消费这些类型，MUST 改为引用新位置，不得保留旧文件副本。
- 单机入口不再经过 `CharacterReplicationSystem.SimulateLocalPlayTick`；删除 LocalPlay 在复制系统上的注册与步进分支。
- **删除** `PlayerController`、`EntityControllerBase`、`AIController` 及预制体上的对应组件。设备输入只经 `LocalInputProvider`。联机预测路径本阶段改为直接采样同一输入源，不得再依赖已删控制器。
- **删除** 迁移后不再被引用的死代码与重复类型（含重复 Identity、旧 `NetworkObjectIdentity` / `EntitySimulationMode`、未使用的 `NetInputProvider`、复制系统 LocalPlay 专用 API）。
- **不**在本变更实现：房间、连接、消息转发、所有权校验、快照编解码、预测/回滚策略迁入 Simulator、Replica 插值、ClientHost/ServerHost。

## Capabilities

### New Capabilities

- `simulator`: 模拟核——Tick、实体注册与 Identity/Role、命令邮箱、`EntityCommand` 构建与 `EntityCharacter` 步进调度、通用生成入口。
- `simulator/local-play`: 单机 Host——装配 LocalPlay 策略、生成并附身本地角色、从设备输入捕获命令、提供可启动/停止的单机会话。

### Modified Capabilities

- （无。`openspec/specs/` 尚无主规格。）

## Impact

- **新增/迁入**：`Assets/Scripts/GamePlay/Simulator/` 下 Tick、Registry、Mailbox、Command（含迁入的构建器）、Spawn（含迁入的生成器）、LocalHost、`Simulator` 生命周期。
- **删除**：`EntitySystem/Controllers/**`、重复 Identity、旧网络身份组件、未引用输入提供器、复制系统 LocalPlay 分支。
- **联机过渡**：`CharacterReplicationSystem` 预测采样改为 `LocalInputProvider`；生成与邮箱引用 Simulator 新路径。SyncTest 开服/开客户端仍可运行。
- **入口**：注册 `Simulator` 并提供单机启动/停止。
- **BREAKING**：场景/预制体上的 `PlayerController` 与 `NetworkObjectIdentity` 组件被移除或替换，旧序列化引用失效。
