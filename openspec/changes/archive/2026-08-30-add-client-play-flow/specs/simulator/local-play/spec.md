## MODIFIED Requirements

### Requirement: Local play is started by gameplay orchestrator
单机模拟会话的启动与停止 SHALL 由房间开战与结束对局发起，且 MUST 发生在活动房间已存在且本机已加入之后。主菜单正式玩法入口 MUST 请求游戏核心，MUST NOT 在按钮回调中直接建房、切场景或开战。调试入口 SHALL 允许逐步请求战局管理器的建房与开战合同，且 MUST NOT 替代主菜单经游戏核心的路径。任何调用方 MUST NOT 在房间开战之外直接启动单机模拟会话作为正式玩法入口。

#### Scenario: Panel starts via orchestrator
- **WHEN** 调用方从单机测试面板请求开始单机且当前房间未开战
- **THEN** 单机模拟会话 MUST 启动，且该启动 MUST 经过战局管理器的开战路径

#### Scenario: Main menu starts via game core
- **WHEN** 玩家从主菜单请求单机开玩
- **THEN** 该请求 MUST 由游戏核心接收，MUST NOT 由主菜单界面直接启动单机模拟会话
