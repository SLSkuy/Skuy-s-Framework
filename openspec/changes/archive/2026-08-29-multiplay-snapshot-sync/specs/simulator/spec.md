## ADDED Requirements

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
