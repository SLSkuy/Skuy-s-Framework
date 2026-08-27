## MODIFIED Requirements

### Requirement: Local session samples device input each tick
单机会话启动并附身本地角色后，MUST 将该会话的本机设备意图来源挂到该角色的注册槽。每个模拟步长 MUST 由模拟核向各槽位收集快照再构建命令并步进。未挂来源的可步进实体 MUST 使用空输入快照。运行时 MUST NOT 存在玩家/AI 实体控制器类型；采样 MUST NOT 经过已删除的控制器组件，MUST NOT 再写入独立命令邮箱，MUST NOT 依赖模拟核上的全局设备字段。

#### Scenario: Held move is applied every tick
- **WHEN** 单机会话运行中且设备持续给出相同移动意图
- **THEN** 每个模拟步长 MUST 将该意图应用于被附身角色

#### Scenario: Player controller types are gone
- **WHEN** 检查玩法程序集与角色原型
- **THEN** MUST NOT 存在玩家控制器、实体控制器基类或 AI 控制器类型，原型上 MUST NOT 残留对应组件
