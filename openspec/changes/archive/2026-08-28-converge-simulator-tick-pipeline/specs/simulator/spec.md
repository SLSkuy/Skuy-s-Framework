## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Possession routes local device input
将本机设备意图赋给某已注册实体 SHALL 通过把设备来源挂到该实体的注册槽完成。每个时钟步长的收集段，仅该槽位 MUST 读到设备快照；其他可步进实体 MUST NOT 因该绑定而读到同一设备快照。模拟核 MUST NOT 再用「至多一个附身 id + 全局设备提供器」完成采样。

#### Scenario: Possessed entity receives device input
- **WHEN** 实体 A 的槽位已挂本机设备来源且该来源给出非空移动意图
- **THEN** 本拍模拟 MUST 按该意图推进实体 A

#### Scenario: Unpossessed entity does not sample device
- **WHEN** 实体 B 已注册但其槽位未挂本机设备来源
- **THEN** 其本拍输入快照 MUST NOT 被本机设备输入覆盖

## REMOVED Requirements

### Requirement: Command mailbox
**Reason**: 推送邮箱在单机路径上没有跨拍或跨模块生命周期，Submit 与消费发生在同一次步进中，无法作为联机窗口缓冲的对接面。
**Migration**: 改为每个 Tick 向注册槽上的意图来源读取快照再模拟；缺采样由该来源返回空快照。不要再调用按标识写入邮箱的入口。联机按 tick 窗口缓冲本阶段可继续走既有复制路径，与槽位上的网络控制器并行存在。
