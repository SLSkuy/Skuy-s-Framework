## ADDED Requirements

### Requirement: Local play is started by gameplay orchestrator
单机会话的启动与停止 SHALL 由玩法会话编排器发起。测试面板或其他调用方 MUST 请求编排器，MUST NOT 在编排器之外直接启动单机模拟会话作为正式玩法入口。

#### Scenario: Panel starts via orchestrator
- **WHEN** 调用方从单机测试面板请求开始单机且编排器空闲
- **THEN** 单机会话 MUST 启动，且该启动 MUST 经过玩法会话编排器

## MODIFIED Requirements

### Requirement: Local session starts without multiplayer
系统 SHALL 提供可启动的单机会话。启动后 MUST 注册并运行模拟核，MUST 生成恰好一个 LocalPlay 角色并对其附身。单机会话 MUST NOT 要求网络客户端或服务端处于运行状态。单机会话 MUST NOT 以已删除的角色复制子系统或已删除的联机 Manager 作为世界时钟或互斥对象；互斥由玩法会话编排器「同时至多一局」保证。

#### Scenario: Start local play spawns possessed pawn
- **WHEN** 调用方经玩法会话编排器启动单机会话且原型可用
- **THEN** 场景中存在一个 LocalPlay 角色，该角色被附身，并且随后的设备移动输入会改变其模拟位置

#### Scenario: Local play does not start net stack
- **WHEN** 单机会话启动成功
- **THEN** 网络客户端与网络服务端 MUST NOT 仅因该启动而被拉起

### Requirement: Local session stop cleans up
停止单机会话 MUST 停止模拟步进、注销并销毁本会话生成的角色、解除附身。停止 MUST 由玩法会话编排器发起。停止后 MUST 允许再次经编排器启动新的单机会话。

#### Scenario: Stop destroys local pawn
- **WHEN** 运行中的单机会话经编排器被停止
- **THEN** 本会话生成的角色 MUST 从场景移除，且后续帧 MUST NOT 再推进已销毁实例

#### Scenario: Restart after stop
- **WHEN** 单机会话停止后再次经编排器启动
- **THEN** 系统 MUST 再次生成并附身一个 LocalPlay 角色

### Requirement: Replication local-play path removed
运行时玩法程序集 MUST NOT 再提供角色复制子系统，MUST NOT 再将本地角色登记为同步实体。单机会话 MUST 只经模拟核步进。

#### Scenario: Local pawn is not a replication entity
- **WHEN** 单机会话正在运行
- **THEN** 系统 MUST NOT 存在仍在推进本地角色的复制子系统注册表
