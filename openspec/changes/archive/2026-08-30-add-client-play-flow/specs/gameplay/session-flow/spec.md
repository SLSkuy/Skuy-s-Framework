## MODIFIED Requirements

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
