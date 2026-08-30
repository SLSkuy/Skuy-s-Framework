## Purpose

由游戏核心承接主菜单玩法意图：编排进房、加载第一张玩法场景、场景就绪后开战，且不把该编排放进按钮、独立流程子系统、玩法场景引导或战局名册模块。

## ADDED Requirements

### Requirement: Menu intent is not business orchestration
主菜单界面 SHALL 仅把玩家点击转发为游戏核心上的玩法入口。主菜单界面 MUST NOT 创建或解散房间、MUST NOT 指定或加载玩法场景名、MUST NOT 监听场景加载完成或失败、MUST NOT 直接开战或启动单机模拟会话。

#### Scenario: Local play button only forwards intent
- **WHEN** 玩家在主菜单请求单机开玩
- **THEN** 系统 MUST 由游戏核心接收该意图，且主菜单界面 MUST NOT 在该点击处理中执行建房、切场景或开战

#### Scenario: Menu panel may be destroyed during load
- **WHEN** 玩家已请求单机开玩且系统正在加载玩法场景
- **THEN** 游戏核心 MUST 仍能完成后续编排，即使主菜单界面已被销毁

### Requirement: Game core owns menu-to-first-map
游戏核心 SHALL 作为主菜单单机开玩的正式入口。收到单机意图后 MUST 经战局管理器确保存在活动房间且本机已加入，MUST 请求加载约定的玩法场景（若当前已不在该场景），MUST NOT 自行维护第二套房间名册。玩法场景名 MUST 由游戏核心或配置持有，MUST NOT 由主菜单界面持有。正式玩法 MUST 从主菜单进入，MUST NOT 依赖玩法场景内的引导组件开战。

#### Scenario: Enter local play from menu loads gameplay scene
- **WHEN** 玩家从主菜单请求单机且当前不在约定玩法场景
- **THEN** 系统 MUST 存在活动房间且本机已加入，且 MUST 开始加载该玩法场景

#### Scenario: Already on gameplay scene starts match
- **WHEN** 玩家从主菜单请求单机且当前已在约定玩法场景且房间未开战且原型可用
- **THEN** 系统 MUST 经战局管理器开战且 MUST NOT 再次加载同一场景作为进入条件

### Requirement: Match starts after gameplay scene is ready
玩法场景加载成功后，游戏核心 SHALL 在活动房间已有本机成员且尚未开战时经战局管理器开战。开战 MUST 至多成功一次。加载失败时 MUST NOT 开战，MUST NOT 仅因此解散房间除非另有显式解散。MUST NOT 用玩法场景引导组件作为开战钩子。

#### Scenario: Scene completed then match starts
- **WHEN** 单机意图已接受且约定玩法场景加载成功且房间未开战且原型可用
- **THEN** 单机模拟会话 MUST 启动

#### Scenario: Scene failed does not start match
- **WHEN** 单机意图已接受且约定玩法场景加载失败
- **THEN** 单机模拟会话 MUST NOT 启动

#### Scenario: Second start after scene ready is rejected
- **WHEN** 玩法场景已就绪且房间已开战且再次请求开战
- **THEN** 系统 MUST 拒绝第二次开战，已有单机模拟会话 MUST 保持

### Requirement: Game core play entry does not own simulation or roster
游戏核心的玩法入口 MUST NOT 直接生成或销毁世界角色，MUST NOT 直接启动或停止模拟核，MUST NOT 在战局管理器之外维护成员表。生成与停止 MUST 仍由房间开战与结束对局触发。

#### Scenario: Entry does not spawn pawn itself
- **WHEN** 游戏核心正在编排单机进入
- **THEN** 系统 MUST NOT 在开战完成前因该入口单独生成 LocalPlay 角色

### Requirement: Game core play entry does not start network or app state manager
游戏核心的玩法入口 MUST NOT 仅因单机进入而拉起网络客户端或网络服务端。本能力 MUST NOT 要求引入应用状态管理器。联机入口 SHALL 与单机入口对称地存在于游戏核心，本阶段 MUST NOT 要求已实现联机业务。

#### Scenario: Local enter does not start net
- **WHEN** 游戏核心成功完成单机进入并开战
- **THEN** 网络客户端与网络服务端 MUST NOT 仅因该路径而被拉起

#### Scenario: Multiplayer button forwards to game core
- **WHEN** 玩家在主菜单请求联机
- **THEN** 主菜单界面 MUST 只转发到游戏核心的联机入口，MUST NOT 在按钮中实现建房或连网
