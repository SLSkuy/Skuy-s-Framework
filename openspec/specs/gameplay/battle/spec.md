# gameplay/battle Specification

## Purpose

用战局管理器持有游戏房间并处理连接抽象，房间名册中的玩家标识独立于传输连接，加入离开不直连传输服务端，为后续真连接与同步留下稳定边界。

## Requirements

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

### Requirement: Battle send uses messenger facade
战局若需向某玩家或全员发送消息，MUST 只通过与传输解耦的发送门面。战局模块 MUST NOT 直接调用传输服务端的发送、广播或启停接口。

#### Scenario: Battle does not call server transport send
- **WHEN** 战局需要通知某玩家加入结果
- **THEN** 发送 MUST 经过发送门面，MUST NOT 由战局直接调用传输服务端发送接口

### Requirement: No world replication in battle this stage
本阶段战局 MUST NOT 广播世界快照，MUST NOT 把玩家输入写入权威模拟缓冲，MUST NOT 生成 Authority 角色或按快照生成 Replica。战局本阶段的职责止于玩家名册与加入/离开。

#### Scenario: Join does not spawn authority pawn
- **WHEN** 战局接受加入
- **THEN** 系统 MUST NOT 仅因此在场景中生成 Authority 角色

#### Scenario: No snapshot broadcast from battle
- **WHEN** 战局正在运行且有至少一名玩家
- **THEN** 系统 MUST NOT 因此周期性广播世界快照

### Requirement: Battle uses paired client and server module handlers
战局相关业务网络收发 SHALL 分别由一个客户端模块处理器与一个服务端模块处理器承担。二者 MUST 为独立类型，MUST NOT 共用同一个处理器类型或同一张登记表。每个模块处理器 MUST 自行将其关心的事件标识登记到对应回调，并在卸载时拆除本模块已登记的回调。战局模块后续新增的业务消息 MUST 并入这一对处理器，MUST NOT 再增加仅处理单条战局命令的处理器类型。

#### Scenario: Server module binds join request
- **WHEN** 服务端模块处理器完成绑定且一条加入请求到达
- **THEN** 该请求 MUST 由该模块处理器的对应回调处理，MUST NOT 依赖「每条协议一个独立处理器类型」作为唯一接收方式

#### Scenario: Client and server handlers stay independent
- **WHEN** 仅绑定服务端模块处理器
- **THEN** 客户端加入响应路径 MUST NOT 因此被登记或调用

#### Scenario: Additional battle events stay on the pair
- **WHEN** 战局需要再处理一条新的业务消息
- **THEN** 系统 MUST 将其登记到已有战局客户端或服务端模块处理器，MUST NOT 新增第三个战局业务处理器类型专服该消息

### Requirement: Battle handler send wrappers
模块处理器向外发送加入相关消息时 SHALL 封装载荷填充与发送门面调用。调用方 MUST NOT 为完成加入回包而直接使用传输入口。战局名册规则（接受、拒绝、玩家标识分配）MUST 仍由战局会话执行，MUST NOT 改由传输层执行。

#### Scenario: Join response goes through facade
- **WHEN** 服务端模块处理器处理加入请求并需要回加入结果
- **THEN** 发送 MUST 经过与传输解耦的发送门面，MUST NOT 由该处理器直接调用传输服务端发送接口

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
