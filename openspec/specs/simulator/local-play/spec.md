# simulator/local-play Specification

## Purpose

在模拟核之上装配单机 Host：生成本地角色、附身设备输入、启动与停止不依赖网络或复制系统的可玩会话。

## Requirements

### Requirement: Local session starts without multiplayer
系统 SHALL 提供可启动的单机模拟会话。启动后 MUST 注册并运行模拟核，MUST 生成恰好一个 LocalPlay 角色并对其附身。单机模拟会话 MUST NOT 要求网络客户端或服务端处于运行状态。单机模拟会话 MUST NOT 以已删除的角色复制子系统或已删除的联机 Manager 作为世界时钟或互斥对象；同时至多一套单机世界由活动房间互斥保证。

#### Scenario: Start local play spawns possessed pawn
- **WHEN** 调用方经战局管理器开战且原型可用且本机已在活动房间中
- **THEN** 场景中存在一个 LocalPlay 角色，该角色被附身，并且随后的设备移动输入会改变其模拟位置

#### Scenario: Local play does not start net stack
- **WHEN** 单机模拟会话启动成功
- **THEN** 网络客户端与网络服务端 MUST NOT 仅因该启动而被拉起

### Requirement: Local play is started by gameplay orchestrator
单机模拟会话的启动与停止 SHALL 由房间开战与结束对局发起，且 MUST 发生在活动房间已存在且本机已加入之后。测试面板或其他调用方 MUST 请求战局管理器，MUST NOT 在房间开战之外直接启动单机模拟会话作为正式玩法入口。

#### Scenario: Panel starts via orchestrator
- **WHEN** 调用方从单机测试面板请求开始单机且当前房间未开战
- **THEN** 单机模拟会话 MUST 启动，且该启动 MUST 经过战局管理器的开战路径

### Requirement: Local session samples device input each tick
单机会话启动并附身本地角色后，MUST 将该会话的本机设备意图来源挂到该角色的注册槽。每个模拟步长 MUST 由模拟核向各槽位收集快照再构建命令并步进。未挂来源的可步进实体 MUST 使用空输入快照。运行时 MUST NOT 存在玩家/AI 实体控制器类型；采样 MUST NOT 经过已删除的控制器组件，MUST NOT 再写入独立命令邮箱，MUST NOT 依赖模拟核上的全局设备字段。

#### Scenario: Held move is applied every tick
- **WHEN** 单机会话运行中且设备持续给出相同移动意图
- **THEN** 每个模拟步长 MUST 将该意图应用于被附身角色

#### Scenario: Player controller types are gone
- **WHEN** 检查玩法程序集与角色原型
- **THEN** MUST NOT 存在玩家控制器、实体控制器基类或 AI 控制器类型，原型上 MUST NOT 残留对应组件

### Requirement: Local session stop cleans up
停止单机模拟会话 MUST 停止模拟步进、注销并销毁本会话生成的角色、解除附身。停止 MUST 由房间结束对局发起。停止单机世界 MUST NOT 单独作为解散房间的替代；解散房间由战局管理器显式执行。结束对局后 MUST 允许再次对同一房间开战。

#### Scenario: Stop destroys local pawn
- **WHEN** 运行中的单机模拟会话经结束对局被停止
- **THEN** 本会话生成的角色 MUST 从场景移除，且后续帧 MUST NOT 再推进已销毁实例

#### Scenario: Restart after stop
- **WHEN** 对局结束后再次经战局管理器开战
- **THEN** 系统 MUST 再次生成并附身一个 LocalPlay 角色

### Requirement: Presentation side effects on possess
单机会话在附身本地角色后 SHALL 将活动相机绑定到该角色的视角节点（若存在），并将光标锁定为游戏游玩模式。解除附身或停止会话 MUST 恢复合理的光标状态。这些副作用 MUST 由单机会话显式执行。

#### Scenario: Camera follows possessed pawn
- **WHEN** 单机会话启动并完成附身
- **THEN** 活动相机 MUST 跟随该角色的视角节点（节点存在时）

### Requirement: Replication local-play path removed
运行时玩法程序集 MUST NOT 再提供角色复制子系统，MUST NOT 再将本地角色登记为同步实体。单机会话 MUST 只经模拟核步进。

#### Scenario: Local pawn is not a replication entity
- **WHEN** 单机会话正在运行
- **THEN** 系统 MUST NOT 存在仍在推进本地角色的复制子系统注册表
