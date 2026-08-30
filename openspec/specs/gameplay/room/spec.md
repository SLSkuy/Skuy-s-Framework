# gameplay/room Specification

## Purpose

提供可跨关卡存活的游戏房间：作为聚会与成员名册的单元，供单机以本机连接加入，并为日后真连接进房留下同一套加入/离开合同。

## Requirements

### Requirement: Room is the gathering unit
系统 SHALL 提供游戏房间作为聚会单元。房间 MUST 具有标识、容量上限与房主玩家。同一时刻由战局管理器管理的本机可玩房间 MUST 至多一间处于活动状态。

#### Scenario: Create room for local play
- **WHEN** 调用方请求创建本机可玩房间且当前没有活动房间
- **THEN** 系统 MUST 创建一间活动房间，且该房间 MUST 具有房主位置

#### Scenario: Second room rejected while one is active
- **WHEN** 已有活动房间且再次请求创建本机可玩房间
- **THEN** 系统 MUST 拒绝创建第二间活动房间，已有房间 MUST 保持活动

### Requirement: Room members are not connection ids
房间 SHALL 为每名已加入成员分配独立于连接标识的玩家标识。同一连接 MUST 在同一房间内至多对应一名玩家。玩家标识 MUST NOT 被规定为等于连接标识。

#### Scenario: Local admit allocates player id
- **WHEN** 房间接受本机连接加入且该连接尚无成员
- **THEN** 房间 MUST 存在该连接对应的一名成员，且其玩家标识与连接标识允许不同

#### Scenario: Duplicate admit rejected
- **WHEN** 已加入的连接再次请求加入同一房间
- **THEN** 系统 MUST 拒绝第二次加入，且 MUST NOT 再分配另一名玩家

### Requirement: Room survives level change
活动房间 MUST 在关卡或场景切换过程中保持活动，直到被显式解散。换关 MUST NOT 清除房间成员表。

#### Scenario: Level change keeps roster
- **WHEN** 房间已有至少一名成员且流程编排器发起关卡切换
- **THEN** 该房间 MUST 仍为活动房间，且已有成员 MUST 仍在名册中

#### Scenario: Dissolve ends the room
- **WHEN** 调用方显式解散活动房间
- **THEN** 该房间 MUST 不再活动，且其成员 MUST 不再视为在房内

### Requirement: Local join does not start network
本机连接加入房间 SHALL 不依赖网络客户端或网络服务端处于运行状态。创建本机可玩房间与本机进房 MUST NOT 仅因此拉起网络客户端或网络服务端。

#### Scenario: Local room without net stack
- **WHEN** 成功创建本机可玩房间并完成本机进房
- **THEN** 网络客户端与网络服务端 MUST NOT 仅因该路径而被拉起
