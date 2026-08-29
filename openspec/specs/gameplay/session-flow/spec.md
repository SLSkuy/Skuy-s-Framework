# gameplay/session-flow Specification

## Purpose

在客户端用玩法会话管理器编排一局从空闲到进行再到结束的流程，驱动单机模拟会话，并与仅负责菜单、加载和暂停的应用状态管理器分开。

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
玩法会话编排器 SHALL 是启动与停止单机模拟会话的唯一玩法入口。开始一局本机游戏 MUST 使单机会话运行；结束本局 MUST 停止单机会话并回到空闲。编排器 MUST NOT 因此拉起网络客户端或网络服务端。

#### Scenario: Start local match
- **WHEN** 调用方通过编排器请求开始单机且当前空闲
- **THEN** 单机模拟会话 MUST 启动，玩法会话 MUST 进入进行中

#### Scenario: Stop local match
- **WHEN** 单机进行中且调用方通过编排器请求结束
- **THEN** 单机模拟会话 MUST 停止，玩法会话 MUST 回到空闲

#### Scenario: Start local does not start network
- **WHEN** 编排器成功开始单机
- **THEN** 网络客户端与网络服务端 MUST NOT 仅因该请求而被拉起

### Requirement: One gameplay session at a time
同一时刻 MUST 至多一局由编排器管理的玩法会话处于进行中。进行中再次请求开始 MUST 失败且 MUST NOT 启动第二套单机模拟会话。

#### Scenario: Second start rejected
- **WHEN** 玩法会话已在进行中且再次请求开始单机
- **THEN** 系统 MUST 拒绝该请求，已有单机会话 MUST 保持运行

### Requirement: No snapshot sync this stage
本阶段玩法会话编排 MUST NOT 启动联机快照同步、MUST NOT 把设备输入作为网络玩家输入发出、MUST NOT 按世界快照生成 Replica。

#### Scenario: No world snapshot consumption
- **WHEN** 玩法会话处于单机进行中
- **THEN** 系统 MUST NOT 因世界快照消息而生成或更新角色
