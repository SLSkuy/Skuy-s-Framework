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

### Requirement: Tick collects intents then simulates
每个时钟步长，模拟核 SHALL 先为所有本拍将步进的实体各收集一份输入快照，再根据已冻结的快照构建命令并推进模拟。收集段 MUST 在任一实体被步进之前完成。意图单位 MUST 仍为可序列化的输入快照。模拟核 MUST NOT 因某实体缺少采样而跳过该步长或回退时钟。

#### Scenario: All snapshots are taken before any step
- **WHEN** 模拟核正在运行且至少有两个可步进实体
- **THEN** 本拍用于构建命令的输入快照 MUST 全部取自步进开始之前的世界，且每个可步进实体本拍 MUST 恰好被推进一次

#### Scenario: Missing sample still advances
- **WHEN** 某可步进实体在本拍没有可用的采样输入
- **THEN** 该实体 MUST 使用空输入快照推进，且该步长 MUST NOT 被跳过

### Requirement: Registration slot owns intent source
每个已注册可步进实体 SHALL 在注册槽上持有本拍意图来源。模拟核 MUST 只向该槽位收集快照，MUST NOT 自行绑定设备模块或按附身标识分支采样。来源未设置时，该槽位 MUST 交出空快照。更换来源 MUST 不影响该槽位已有的命令边沿基线，除非该实体被注销后重新注册。

#### Scenario: Core collects only from the slot
- **WHEN** 某已注册实体的槽位已挂上意图来源且来源给出非空移动快照
- **THEN** 本拍模拟 MUST 按该快照推进该实体，且 MUST NOT 要求模拟核持有全局设备提供器

#### Scenario: Unset source yields empty snapshot
- **WHEN** 某可步进实体已注册但槽位未设置意图来源
- **THEN** 其本拍 MUST 按空输入快照推进

### Requirement: Intent source is snapshot-only
意图来源 SHALL 仅通过读取当前输入快照向模拟核供数（与已有 `GetInputState` 契约一致）。设备来源与网络控制器 MUST 实现同一读取口。无可用采样时，来源 MUST 交出默认空快照。模拟核 MUST NOT 解释收集成功、失败、延迟或丢包。

#### Scenario: Network controller missing packet uses empty snapshot
- **WHEN** 槽位挂的是网络意图来源且本拍没有可用采样
- **THEN** 模拟核 MUST 仍完成本拍步进，且 MUST 将该实体视为收到空输入快照

#### Scenario: Device and network share the same read contract
- **WHEN** 调用方把设备来源或网络控制器赋给不同实体的槽位
- **THEN** 收集段 MUST 对二者使用同一快照读取方式，MUST NOT 按来源种类分支

### Requirement: Registration owns command edges
每个已注册可步进实体 SHALL 在该次注册生命周期内保有自己的命令边沿状态（按住/按下/松开）。注销 MUST 丢弃该边沿状态。同一标识再次注册 MUST 从干净边沿基线开始。模拟核 MUST NOT 在注册表之外再维护一套仅按标识索引的独立边沿存储。

#### Scenario: Re-register resets edges
- **WHEN** 某实体被注销后以同一标识重新注册并再次步进
- **THEN** 本拍命令边沿 MUST 不继承注销前的按住状态

### Requirement: Possession routes local device input
将本机设备意图赋给某已注册实体 SHALL 通过把设备来源挂到该实体的注册槽完成。每个时钟步长的收集段，仅该槽位 MUST 读到设备快照；其他可步进实体 MUST NOT 因该绑定而读到同一设备快照。模拟核 MUST NOT 再用「至多一个附身 id + 全局设备提供器」完成采样。

#### Scenario: Possessed entity receives device input
- **WHEN** 实体 A 的槽位已挂本机设备来源且该来源给出非空移动意图
- **THEN** 本拍模拟 MUST 按该意图推进实体 A

#### Scenario: Unpossessed entity does not sample device
- **WHEN** 实体 B 已注册但其槽位未挂本机设备来源
- **THEN** 其本拍输入快照 MUST NOT 被本机设备输入覆盖

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

### Requirement: Replica entities skip command simulation
身份为 Replica 的已注册实体 SHALL 不参与本拍命令收集后的模拟步进。模拟核 MUST NOT 对其调用基于输入快照的 `Step`。LocalPlay 与 Authority 仍按现有收集-再-步进规则推进。

#### Scenario: Replica is not stepped from commands
- **WHEN** 已注册实体的模拟角色为 Replica 且时钟正在推进
- **THEN** 该实体本拍 MUST NOT 因槽位输入快照而被步进

#### Scenario: Authority still steps
- **WHEN** 已注册实体的模拟角色为 Authority 且槽位有或没有采样
- **THEN** 该实体本拍 MUST 仍被推进一次（无采样则空输入）

### Requirement: Restore simulated state without network
模拟核 SHALL 允许将完整回滚状态写回已注册且已初始化的实体。该写入 MUST 不依赖网络消息类型。过旧的调用方序号由 Host 丢弃，不由核解释。

#### Scenario: Restore matches captured pose
- **WHEN** 对已注册实体捕获状态后再 Restore 同一状态
- **THEN** 该实体的位置与视角 MUST 与捕获时一致

### Requirement: Authority network source consumes input buffer
服务端 Authority 槽位的网络意图来源 SHALL 按客户端输入序号窗口入队，并在收集段通过同一快照读取口交出下一拍应消费的输入（缺失则为空快照）。收集段 MUST NOT 按来源种类分支。命令边沿 MUST 仍只存在于注册槽的构建器中。

#### Scenario: Enqueued input is consumed in order
- **WHEN** 某 Authority 实体已按序号 N 入队非空输入且下一消费点为 N
- **THEN** 本拍收集 MUST 将该快照用于构建该实体命令

#### Scenario: Gap uses empty snapshot
- **WHEN** 下一消费序号在窗口内但槽位没有对应入队
- **THEN** 本拍 MUST 按空输入快照推进并仍前进消费点
