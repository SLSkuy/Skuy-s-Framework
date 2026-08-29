## Purpose

把到达的业务网络消息路由到可独立注册与注销的处理器，使玩法只与处理器及窄门面交互，而不覆盖整条事件槽或直接操作传输入口。

## ADDED Requirements

### Requirement: Multiple handlers per event
系统 SHALL 允许同一消息事件同时登记多个处理器。后登记 MUST NOT 覆盖或丢弃先登记的处理器。分发时 MUST 将同一条已解码消息交给该事件上所有仍登记的处理器。

#### Scenario: Two handlers both receive
- **WHEN** 同一事件已登记两个处理器且该事件的一条消息到达
- **THEN** 两个处理器 MUST 都收到该消息

#### Scenario: Second register does not replace first
- **WHEN** 事件 A 已有处理器甲，再登记处理器乙到事件 A
- **THEN** 甲 MUST 仍保持登记

### Requirement: Unregister by handler instance
注销 MUST 只移除指定的处理器实例。注销 MUST NOT 清除该事件上其余仍登记的处理器。未登记过的实例被注销时 MUST 为无操作且 MUST NOT 影响该事件的分发。

#### Scenario: Unregister one of two
- **WHEN** 事件 A 上登记了甲与乙，且仅注销甲
- **THEN** 随后到达的事件 A 消息 MUST 只交给乙，MUST NOT 交给甲

#### Scenario: Unregister missing instance is noop
- **WHEN** 调用方注销一个从未登记的处理器
- **THEN** 系统 MUST NOT 抛出错误，且已有处理器的分发 MUST 不变

### Requirement: Business handlers do not use transport entry
业务消息处理器 SHALL 只通过消息分发入口接收已解码消息（服务端含发送方连接标识）。处理器处理业务时 MUST NOT 查询连接表、MUST NOT 启停传输、MUST NOT 按会话标识直接向传输层发送。需要回包或广播时 MUST 使用与传输解耦的发送门面。

#### Scenario: Handler cannot require server transport
- **WHEN** 某业务处理器处理一条加入或离开相关消息
- **THEN** 该处理路径 MUST 不读取传输服务端子系统的连接表或会话绑定

### Requirement: Connection protocol stays in transport
心跳、Ping/Pong 与可靠连接握手类消息 SHALL 仍由传输层内部处理。它们 MUST NOT 登记为玩法业务处理器，MUST NOT 进入战局或玩法会话编排。

#### Scenario: Heartbeat does not reach battle
- **WHEN** 客户端发出心跳请求且传输层正在运行
- **THEN** 战局会话 MUST NOT 因该心跳而创建、更新或移除玩家
