## ADDED Requirements

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
