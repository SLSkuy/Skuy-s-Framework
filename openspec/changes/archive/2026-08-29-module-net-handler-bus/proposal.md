## Why

现有网络分发按 Handler **实例**登记，且同一实例只能占一个事件槽，导致玩法被拆成「一条协议一个类」，收发与注销都散落。需要在网络总线内改为 `EventID` 绑定具体 Action、按 Action 精确注销，并用客户端/服务端各一个整包模块 Handler 收拢战局相关收发。

## What Changes

- 网络消息分发改为以 `(NetEvent, Action)` 为一条登记：同一事件可挂多条 Action；注销指定 Action MUST NOT 清掉该事件其余监听。
- 分发器只维护 `EventID →` 回调列表。注销必须带事件标识：`Unregister(eventId, callback)`。MUST NOT 维护 Action→Event 反向索引，MUST NOT 提供「只凭回调拆掉其挂过的全部事件」。
- **BREAKING**：`MessageProcessor` / `NetClient` / `NetServer` 不再以 Handler 实例为唯一登记键；`Register(event, INetHandler<T>)` 与 `Unregister(object)` 替换为登记/注销具体方法组。
- 玩法侧：战局整包一个客户端 Handler、一个服务端 Handler（两套类型互不继承）；Handler 自行 `Bind` 一串 `Register(eventId, OnXxx)`，并封装对应 `SendXxx`。删除「一条加入协议一个 `GameJoinRequestHandler`」作为主路径。
- **约定（后续一律）**：每个功能模块至多一对业务 Handler（客户端一个类型、服务端一个类型）。后续新增业务消息 MUST 加到该模块 Handler 的方法上，MUST NOT 再为单条命令/单条 `NetEvent` 新增独立业务 Handler 类型。传输层连接协议仍挂在 `NetClient`/`NetServer` 内部方法组上，不算模块 Handler。
- MUST NOT 与本地 `EventBus`（`Get<T>()`）合并；连接协议（心跳、Ping/Pong、可靠握手）仍由传输内部登记，玩法 `Unbind` MUST NOT 摘掉它们。

## Capabilities

### New Capabilities

- （无）本变更只调整已有网络分发与战局接线的行为合同。

### Modified Capabilities

- `network/message-dispatch`: 登记键从 Handler 实例改为 EventID 上的 Action 列表；同事件多播；按 `(事件, 回调)` 注销；客户端与服务端分表；玩法业务 MUST 按模块整包 Handler，禁止一条命令一个 Handler 类型。
- `gameplay/battle`: 加入相关收发由整包模块 Handler 绑定与封装；名册仍在战局会话；不经传输入口直发；战局后续协议同样并入这一对类型。

## Impact

- **Network**：改 `MessageProcessor`、`NetClient.RegisterHandler` / `UnregisterHandler`、`NetServer` 对应 API；传输内部心跳等改为向总线登记方法组。
- **GamePlay**：新增战局客户端/服务端 Handler；`BattleSession` 仍管名册；发送继续走 `IMatchMessenger` 等窄门面。
- **Tests**：重写 `MessageProcessorTests` 与加入路径测试，覆盖同事件多 Action、只删一条。
- **Non-goals**：不改 `.proto`、不实现快照/输入同步、不改本地事件总线、不把客户端与服务端 Handler 合成一个类型。
