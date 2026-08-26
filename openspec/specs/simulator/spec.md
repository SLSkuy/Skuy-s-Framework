# simulator Specification

## Purpose

提供与联机无关的实体模拟核：固定步长推进世界、注册可模拟实体、接收每拍意图并驱动角色状态，供单机 Host 与后续多人 Host 共用。

## Requirements

### Requirement: Fixed-step simulation clock
模拟核 SHALL 使用单一固定步长时钟推进已注册实体。时钟频率 MUST 来自模拟配置。同一进程内 MUST NOT 再为 LocalPlay 启用第二套独立模拟时钟。

#### Scenario: Tick advances registered entities
- **WHEN** 模拟核正在运行且已注册至少一个可步进实体
- **THEN** 每个模拟步长对该实体应用恰好一次本拍命令并推进其模拟状态

#### Scenario: No tick when stopped
- **WHEN** 模拟核已停止或尚未启动
- **THEN** 实体模拟状态 MUST NOT 因帧更新而被推进

### Requirement: Entity registration
模拟核 SHALL 按非零实体标识注册与注销可模拟对象。每个标识 MUST 最多对应一个已注册实例。注册时 MUST 记录该实例的模拟角色（本阶段为 LocalPlay）。注销后 MUST 停止对该实例的步进。

#### Scenario: Duplicate id rejected
- **WHEN** 调用方尝试用已被占用的实体标识再次注册另一个实例
- **THEN** 系统 MUST 拒绝该注册并保持原实例不变

#### Scenario: Unregister stops simulation
- **WHEN** 已注册实体被注销
- **THEN** 后续时钟步进 MUST NOT 再推进该实体

### Requirement: Command mailbox
模拟核 SHALL 为每个已注册实体提供按模拟序号对齐的意图邮箱。本拍步进 MUST 消费该实体对应序号的意图；若本拍没有写入，MUST 使用空意图并仍推进模拟。意图单位 MUST 为可序列化的输入快照，以便后续多人会话写入同一邮箱而不改步进公式。

#### Scenario: Written input is consumed on matching tick
- **WHEN** 某实体在序号 N 的邮箱中已有输入快照
- **THEN** 步进 N 时 MUST 使用该快照构建命令并推进该实体

#### Scenario: Missing input uses empty command
- **WHEN** 某实体在序号 N 的邮箱中没有输入快照
- **THEN** 步进 N 时 MUST 使用空意图推进，且 MUST NOT 跳过该步长

### Requirement: Possession routes local device input
模拟核 SHALL 支持将设备输入附身到至多一个已注册实体。每个时钟步长，仅被附身实体 MUST 从本机设备输入提供器采样并写入自己的邮箱；未被附身的实体 MUST NOT 从本机设备采样。

#### Scenario: Possessed entity receives device input
- **WHEN** 实体 A 被附身且设备输入提供器给出非空移动意图
- **THEN** 下一模拟步长 MUST 按该意图推进实体 A

#### Scenario: Unpossessed entity does not sample device
- **WHEN** 实体 B 已注册但未被附身
- **THEN** 其本拍邮箱 MUST NOT 被本机设备输入覆盖

### Requirement: Shared spawn entry
模拟核 SHALL 提供与传输无关的生成入口：根据原型创建实例、写入身份与模拟角色、初始化实体模拟、完成注册。生成失败（缺少原型或缺少实体组件）MUST 失败且 MUST NOT 留下半注册实例。

#### Scenario: Successful spawn is registered
- **WHEN** 生成入口使用有效原型与未占用的实体标识成功创建角色
- **THEN** 该实例 MUST 可被时钟步进，且身份中的模拟角色 MUST 与请求一致

#### Scenario: Failed spawn does not register
- **WHEN** 原型无效或实例缺少实体模拟组件
- **THEN** 系统 MUST 不注册该标识，场景中 MUST NOT 残留未初始化的半成品实例

### Requirement: Capture without network
模拟核 SHALL 允许读取已注册实体的完整回滚状态快照。该能力 MUST 不依赖网络消息。本阶段无确认/插值消费方，但接口 MUST 存在以便后续 Host 复用。

#### Scenario: Capture returns current simulated pose
- **WHEN** 已注册实体已被至少推进过一次
- **THEN** 捕获接口 MUST 返回与当前模拟一致的位置与视角状态

### Requirement: Shared types live under simulator
生成、命令构建与按 tick 意图缓冲 SHALL 作为模拟核的一部分存在于 Simulator 模块路径下。MultiPlay MUST NOT 再保留这些类型的旧副本；若联机路径仍需要它们，MUST 引用 Simulator 中的同一实现。

#### Scenario: Spawn and command types have a single home
- **WHEN** 单机或联机生成角色并构建本拍命令
- **THEN** 生成与命令构建 MUST 使用 Simulator 模块中的实现，且旧 MultiPlay 生成器/命令缓冲文件 MUST NOT 仍作为第二份实现存在

### Requirement: Single identity component
可模拟对象的身份 SHALL 仅由 Simulator 身份组件表达。重复的身份接口、旧网络身份组件与旧模拟角色枚举 MUST NOT 保留在运行时程序集中。

#### Scenario: Prefab uses simulator identity
- **WHEN** 角色原型被实例化
- **THEN** 实例 MUST 带有 Simulator 身份组件，且 MUST NOT 带有已删除的旧网络身份组件
