## MODIFIED Requirements

### Requirement: Multiple handlers per event
系统 SHALL 允许同一消息事件同时登记多条回调。后登记 MUST NOT 覆盖或丢弃先登记的回调。分发时 MUST 将同一条已解码消息交给该事件上所有仍登记的回调。客户端回调与服务端回调 MUST 使用分开的登记表，MUST NOT 混在同一张表中分发。

#### Scenario: Two handlers both receive
- **WHEN** 同一事件已登记两条回调且该事件的一条消息到达
- **THEN** 两条回调 MUST 都被调用

#### Scenario: Second register does not replace first
- **WHEN** 事件 A 已有回调甲，再登记回调乙到事件 A
- **THEN** 甲 MUST 仍保持登记

## REMOVED Requirements

### Requirement: Unregister by handler instance
**Reason**: 以处理器实例为唯一登记键时，同一实例无法挂到多个事件，也无法按单条回调精确拆除。
**Migration**: 改为按 `(事件, 回调)` 登记与注销；需要卸掉某模块全部监听时拆除该模块 Bind 过的全部对。

## ADDED Requirements

### Requirement: Bind event id to a specific callback
调用方 SHALL 将消息事件标识与一条具体回调成对登记。同一事件标识 MUST 能同时绑定多条不同回调。同一 `(事件, 回调)` 重复登记 MUST 为无操作。客户端回调签名 MUST 只接收已解码消息；服务端回调签名 MUST 另含发送方连接标识。

#### Scenario: Event binds to one method
- **WHEN** 调用方将事件 A 登记到方法甲，且事件 A 的消息到达
- **THEN** 系统 MUST 调用甲，MUST NOT 要求存在独立的「每协议一个处理器类型」才能收到该消息

#### Scenario: Duplicate pair is noop
- **WHEN** 同一事件与同一回调已被登记，再次以该对登记
- **THEN** 随后一条该事件消息 MUST 只使该回调被调用一次

### Requirement: Unregister a specific callback
注销 MUST 只移除指定事件上的指定回调。注销 MUST NOT 清除该事件上其余仍登记的回调。未在该事件上登记过的回调被注销时 MUST 为无操作且 MUST NOT 影响该事件的分发。注销 MUST 同时给出事件标识与回调，MUST NOT 仅凭回调拆除其在其他事件上的登记。

#### Scenario: Unregister one of two
- **WHEN** 事件 A 上登记了回调甲与乙，且仅注销事件 A 上的甲
- **THEN** 随后到达的事件 A 消息 MUST 只交给乙，MUST NOT 交给甲

#### Scenario: Unregister missing callback is noop
- **WHEN** 调用方注销事件 A 上一条从未登记到该事件的回调
- **THEN** 系统 MUST NOT 抛出错误，且已有回调的分发 MUST 不变

### Requirement: Module unbind does not clear other listeners
玩法模块卸载其网络监听时 SHALL 只拆除该模块登记过的回调。传输层为心跳、Ping/Pong 与可靠连接握手登记的回调 MUST 仍保持登记。

#### Scenario: Battle unbind leaves heartbeat registered
- **WHEN** 传输层已为心跳登记回调，且玩法模块拆除自身全部网络登记
- **THEN** 随后到达的心跳消息 MUST 仍由传输层回调处理

### Requirement: Business handlers are module scoped
玩法与功能模块的业务网络消息 SHALL 由该模块的客户端模块处理器与服务端模块处理器接收并封装发送（每个角色至多一个业务处理器类型）。系统 MUST NOT 为单条消息事件或单条命令新增独立的业务处理器类型。同一模块的多条事件 MUST 登记到该模块处理器上的不同回调。传输层连接协议（心跳、Ping/Pong、可靠握手）MUST NOT 算作玩法模块处理器，MUST 仍由传输内部回调处理。

#### Scenario: New command stays on the module handler
- **WHEN** 某已有玩法模块需要处理一条新的业务消息事件
- **THEN** 该事件 MUST 绑定到该模块已有客户端或服务端处理器上的回调，MUST NOT 因此新增仅服务该事件的业务处理器类型

#### Scenario: One command one handler type is forbidden
- **WHEN** 检查玩法业务接收入口
- **THEN** MUST NOT 存在「一个消息事件对应一个独立业务处理器类型」的接收入口（测试替身除外）
