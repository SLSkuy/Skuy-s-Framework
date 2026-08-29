## Purpose

提供与单机 Host 同构的快照同步联机会话：一场一房管理成员，客户端只发送输入并应用权威快照，服务端独占模拟步进。不包含预测与回滚重放。

## ADDED Requirements

### Requirement: One match room
联机会话 SHALL 维护至多一场比赛房间：成员为连接标识到实体标识与所有者的映射。加入成功 MUST 在服务端生成 Authority 角色并登记。离开或断线 MUST 注销并销毁该成员实体。房间 MUST 能判定某连接是否有权为某实体提交输入。

#### Scenario: Join spawns authority pawn
- **WHEN** 服务端接受某连接的加入且该连接尚无实体
- **THEN** 场景中存在该连接拥有的 Authority 角色且房间能按连接查到其实体标识

#### Scenario: Leave unregisters pawn
- **WHEN** 已加入的连接断开或离开
- **THEN** 其角色 MUST 从模拟核注销并从场景移除

#### Scenario: Unauthorized input rejected
- **WHEN** 连接为实体 A 提交输入但其不是 A 的所有者
- **THEN** 该输入 MUST NOT 进入该实体的权威缓冲

### Requirement: Client sends input and applies snapshots
客户端会话运行时，MUST 按模拟步长采样本机设备输入并发送玩家输入消息，MUST NOT 把设备来源挂到任何 Replica 槽位。收到世界快照后，未知实体 MUST 生成为 Replica 并注册；已知实体 MUST 将快照状态 Restore 到该实体。客户端已注册的角色本阶段 MUST 均为 Replica。

#### Scenario: Local move is not applied before snapshot
- **WHEN** 客户端会话运行且设备给出移动意图
- **THEN** 在对应权威快照到达并 Restore 之前，己方 Replica MUST NOT 因该设备意图被命令步进

#### Scenario: Snapshot spawns missing replica
- **WHEN** 快照包含尚未注册的实体标识
- **THEN** 客户端 MUST 生成 Replica 角色并注册到模拟核

#### Scenario: Newer snapshot updates pose
- **WHEN** 已注册 Replica 收到序号更新的角色快照
- **THEN** 其模拟位姿 MUST 变为该快照所表达的状态

### Requirement: Hosts mirror local session shape
服务端与客户端会话 SHALL 各自持有一份模拟核，在 Init 中创建，在 Update 中驱动，在停止时注销实体并停钟。联机会话 MUST NOT 以角色复制子系统为世界时钟。单机会话与联机会话 MUST 互斥。联机 MUST NOT 启动预测步进或回滚重放。

#### Scenario: Server clock steps authority only
- **WHEN** 服务端会话运行且至少有一个 Authority 成员
- **THEN** 模拟核 MUST 推进这些 Authority，且 MUST NOT 依赖复制子系统的 Tick 回调

#### Scenario: Starting multiplay does not start local play
- **WHEN** 联机会话启动成功
- **THEN** 单机会话 MUST NOT 同时处于运行状态

#### Scenario: No prediction path
- **WHEN** 客户端生成己方角色
- **THEN** 其模拟角色 MUST 为 Replica，MUST NOT 为 Predict

### Requirement: Existing sync test panel drives multiplay
联机启动与停止 SHALL 由现有同步测试面板完成（开服、开客、停止）。该面板 MUST 接到新的联机会话，MUST NOT 再以角色复制子系统的实体数与 Tick 作为主状态。本能力 MUST NOT 依赖新建的专用联机 GUI。

#### Scenario: Start server from existing panel
- **WHEN** 调用方在现有同步测试面板点击开启服务端且单机未运行
- **THEN** 服务端会话 MUST 启动，面板 MUST 显示服务端运行中

#### Scenario: Start client from existing panel
- **WHEN** 调用方在现有同步测试面板点击开启客户端且单机未运行
- **THEN** 客户端会话 MUST 启动，面板 MUST 显示连接或 ClientId 状态
