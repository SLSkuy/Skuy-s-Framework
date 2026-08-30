# gameplay/session-flow Specification

## Purpose

在客户端用流程编排器在进房之后驱动关卡与单机模拟会话，并与应用壳状态分开；正式单机开玩必须经过活动房间。

## Requirements

### Requirement: Gameplay session is not application shell state
系统 SHALL 提供独立于应用状态管理器的玩法会话编排。应用状态管理器 MUST 仍只表达菜单、加载、游玩壳层与暂停。玩法会话相位（空闲、进行中、结束）MUST NOT 用应用状态枚举代替。

#### Scenario: Pause does not equal session stop
- **WHEN** 单机会话正在进行且应用进入暂停
- **THEN** 玩法会话 MUST 仍视为进行中，直到编排器显式停止该会话

#### Scenario: Menu state without gameplay session
- **WHEN** 应用处于主菜单且未请求开始一局
- **THEN** 玩法会话 MUST 处于空闲，且单机模拟会话 MUST NOT 运行

### Requirement: Orchestrator starts and stops local play
战局管理器 SHALL 是开战与结束对局的正式合同入口。开战 MUST 使单机模拟会话运行；结束对局 MUST 停止单机模拟会话且 MUST NOT 要求解散房间。开战 MUST NOT 因此拉起网络客户端或网络服务端。战局管理器 MUST NOT 承担主菜单到第一张玩法场景的加载。主菜单单机进入 SHALL 由游戏核心编排，并在场景就绪后调用开战合同。

#### Scenario: Start local match
- **WHEN** 调用方经战局管理器对已有成员的活动房间开战且原型可用
- **THEN** 单机模拟会话 MUST 启动

#### Scenario: Stop local match
- **WHEN** 对局进行中且调用方结束对局
- **THEN** 单机模拟会话 MUST 停止，关卡流程对象 MUST 不再由该房间持有

#### Scenario: Start local does not start network
- **WHEN** 房间成功开战
- **THEN** 网络客户端与网络服务端 MUST NOT 仅因该请求而被拉起

#### Scenario: Battle does not load first map
- **WHEN** 调用方经战局管理器开战
- **THEN** 该调用 MUST NOT 发起主菜单到玩法场景的场景加载

### Requirement: One gameplay session at a time
同一活动房间同一时刻 MUST 至多一套对局。对局进行中再次开战 MUST 失败且 MUST NOT 启动第二套单机模拟会话。

#### Scenario: Second start rejected
- **WHEN** 房间已开战且再次请求开战
- **THEN** 系统 MUST 拒绝该请求，已有单机模拟会话与活动房间 MUST 保持

### Requirement: No snapshot sync this stage
本阶段玩法会话编排 MUST NOT 启动联机快照同步、MUST NOT 把设备输入作为网络玩家输入发出、MUST NOT 按世界快照生成 Replica。

#### Scenario: No world snapshot consumption
- **WHEN** 玩法会话处于单机进行中
- **THEN** 系统 MUST NOT 因世界快照消息而生成或更新角色

### Requirement: Local play goes through a room
正式单机玩法 MUST 先经战局管理器得到活动房间且本机已加入，再由该房间开战。MUST NOT 在无活动房间或本机未进房时启动单机模拟会话作为正式玩法。MUST NOT 由关卡流程对象创建房间。主菜单路径 MUST 由游戏核心调用战局管理器完成建房与进房，MUST NOT 由主菜单界面直接维护名册。

#### Scenario: Start local creates room then match
- **WHEN** 调用方经战局管理器创建本机房间、完成本机进房并开战且原型可用
- **THEN** 该房间 MUST 持有关卡流程对象与单机模拟会话

#### Scenario: Start without room is not a host-only shortcut
- **WHEN** 不存在活动房间
- **THEN** 系统 MUST NOT 将「仅启动单机模拟会话」视为已完成的正式单机开玩

#### Scenario: Menu path uses game core then battle
- **WHEN** 玩家从主菜单请求单机开玩且随后玩法场景就绪且原型可用
- **THEN** 活动房间与本机成员 MUST 已由战局管理器登记，且开战 MUST 仍经战局管理器

### Requirement: Room owns in-match flow
房间开战之后 SHALL 持有关卡流程对象与模拟核。关卡流程对象 MUST 只表达战局内相位（至少包括未在关卡中、关卡进行中、关卡切换中），MUST NOT 创建或解散房间。关卡切换 MUST NOT 解散活动房间，MUST NOT 结束对局。

#### Scenario: In level after match start
- **WHEN** 房间成功开战且原型可用
- **THEN** 关卡流程 MUST 处于关卡进行中，且单机模拟会话 MUST 运行

#### Scenario: Change level keeps room
- **WHEN** 对局进行中且关卡流程对象请求切换关卡
- **THEN** 该房间 MUST 保持活动且仍持有流程对象，关卡流程 MUST 经过关卡切换中，且完成后 MUST 回到关卡进行中

#### Scenario: End match keeps roster
- **WHEN** 对局进行中且调用方结束对局
- **THEN** 单机模拟会话与关卡流程对象 MUST 拆除，活动房间与成员 MUST 仍在
