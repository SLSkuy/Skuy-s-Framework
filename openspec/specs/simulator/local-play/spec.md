# simulator/local-play Specification

## Purpose

在模拟核之上装配单机 Host：生成本地角色、附身设备输入、启动与停止不依赖网络或复制系统的可玩会话。

## Requirements

### Requirement: Local session starts without multiplayer
系统 SHALL 提供可启动的单机会话。启动后 MUST 注册并运行模拟核，MUST 生成恰好一个 LocalPlay 角色并对其附身。单机会话 MUST NOT 要求网络客户端或服务端处于运行状态，MUST NOT 以角色复制子系统作为世界时钟。

#### Scenario: Start local play spawns possessed pawn
- **WHEN** 调用方启动单机会话且原型可用
- **THEN** 场景中存在一个 LocalPlay 角色，该角色被附身，并且随后的设备移动输入会改变其模拟位置

#### Scenario: Local play does not start net stack
- **WHEN** 单机会话启动成功
- **THEN** 网络客户端与网络服务端 MUST NOT 仅因该启动而被拉起

### Requirement: Local session samples device input each tick
单机会话启动并附身本地角色后，MUST 将该会话的本机设备意图来源挂到该角色的注册槽。每个模拟步长 MUST 由模拟核向各槽位收集快照再构建命令并步进。未挂来源的可步进实体 MUST 使用空输入快照。运行时 MUST NOT 存在玩家/AI 实体控制器类型；采样 MUST NOT 经过已删除的控制器组件，MUST NOT 再写入独立命令邮箱，MUST NOT 依赖模拟核上的全局设备字段。

#### Scenario: Held move is applied every tick
- **WHEN** 单机会话运行中且设备持续给出相同移动意图
- **THEN** 每个模拟步长 MUST 将该意图应用于被附身角色

#### Scenario: Player controller types are gone
- **WHEN** 检查玩法程序集与角色原型
- **THEN** MUST NOT 存在玩家控制器、实体控制器基类或 AI 控制器类型，原型上 MUST NOT 残留对应组件

### Requirement: Local session stop cleans up
停止单机会话 MUST 停止模拟步进、注销并销毁本会话生成的角色、解除附身。停止后 MUST 允许再次启动新的单机会话。

#### Scenario: Stop destroys local pawn
- **WHEN** 运行中的单机会话被停止
- **THEN** 本会话生成的角色 MUST 从场景移除，且后续帧 MUST NOT 再推进已销毁实例

#### Scenario: Restart after stop
- **WHEN** 单机会话停止后再次启动
- **THEN** 系统 MUST 再次生成并附身一个 LocalPlay 角色

### Requirement: Presentation side effects on possess
单机会话在附身本地角色后 SHALL 将活动相机绑定到该角色的视角节点（若存在），并将光标锁定为游戏游玩模式。解除附身或停止会话 MUST 恢复合理的光标状态。这些副作用 MUST 由单机会话显式执行。

#### Scenario: Camera follows possessed pawn
- **WHEN** 单机会话启动并完成附身
- **THEN** 活动相机 MUST 跟随该角色的视角节点（节点存在时）

### Requirement: Replication local-play path removed
角色复制子系统 MUST NOT 再提供或执行 LocalPlay 注册与步进。单机会话 MUST NOT 把本地角色注册进复制系统。

#### Scenario: Local pawn is not a replication entity
- **WHEN** 单机会话正在运行且未启动联机端点
- **THEN** 复制系统 MUST NOT 将该本地角色计为已注册同步实体，MUST NOT 推进其模拟
