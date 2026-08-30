## ADDED Requirements

### Requirement: Battle manager owns rooms
系统 SHALL 提供战局管理器作为房间的拥有者与连接抽象入口。创建、解散房间以及将连接翻译为房间成员 MUST 经战局管理器，MUST NOT 由单机模拟会话或流程编排器直接维护第二套成员表。

#### Scenario: Admit goes through battle manager
- **WHEN** 本机连接请求加入本机可玩房间
- **THEN** 成员登记 MUST 由战局管理器写入该房间名册

#### Scenario: Host does not own roster
- **WHEN** 单机模拟会话正在运行
- **THEN** 房间成员集合 MUST 仍以战局管理器所持房间为准，MUST NOT 以模拟会话内部表替代

### Requirement: Room join does not spawn
战局管理器接受连接加入房间时 MUST NOT 仅因此在场景中生成角色。生成与销毁世界角色 MUST 仍由单机模拟会话在流程编排器请求启动或停止时执行。

#### Scenario: Admit does not spawn pawn
- **WHEN** 战局管理器接受本机连接加入且尚未启动单机模拟会话
- **THEN** 系统 MUST NOT 仅因此在场景中生成 LocalPlay 或 Authority 角色

## MODIFIED Requirements

### Requirement: Player identity is not connection identity
战局 SHALL 通过其所持房间为每名已加入玩家分配独立于连接标识的玩家标识。同一连接 MUST 在同一活动房间内至多对应一名玩家。玩家标识 MUST NOT 被规定为等于连接标识。

#### Scenario: Join allocates player id
- **WHEN** 战局管理器接受某连接加入活动房间且该连接尚无玩家
- **THEN** 该房间 MUST 存在一名该连接对应的玩家，且其玩家标识与连接标识允许不同

#### Scenario: Duplicate join rejected
- **WHEN** 已加入的连接再次请求加入同一活动房间
- **THEN** 系统 MUST 拒绝第二次加入，且 MUST NOT 再分配另一名玩家

### Requirement: Leave is notified by battle not transport
连接被彻底移除时，系统 SHALL 将其翻译为房间成员离开并通知战局管理器。战局管理器处理离开时 MUST NOT 读取传输服务端的连接表或会话绑定。未加入房间的连接被移除时 MUST NOT 要求房间移除玩家。本机假连接离开 MUST 走同一套离开合同。

#### Scenario: Removed connection leaves player
- **WHEN** 已加入活动房间的连接被移除
- **THEN** 该房间 MUST 移除对应玩家，且该路径 MUST 不查询传输服务端子系统

#### Scenario: Unjoined connection removal is ignored by roster
- **WHEN** 从未加入房间的连接被移除
- **THEN** 房间玩家集合 MUST 不变

#### Scenario: Local leave without transport
- **WHEN** 本机假连接离开活动房间
- **THEN** 对应成员 MUST 从名册移除，且 MUST NOT 因此要求网络服务端处于运行状态
