## Context

动机见 `proposal.md`。现状：`MessageProcessor` 用 `List<object>` 存 Handler 实例，`_bindings` 以实例为唯一键，同一实例无法登记多个 `NetEvent`；热路径每次 `ToArray()`。玩法示例是 `IServerNetHandler<Game_Join_Request>` 单类，发送在 `BattleSession`。本地 `EventBus.Get<T>()` 保持不动。行为合同见本 change 的 specs。

## Goals / Non-Goals

**Goals:**

- 分发键改为 `(NetEvent, 方法组)`：同一事件多条回调，按该事件成对拆除。
- 客户端 / 服务端两套登记 API 与两张表，不共用基类接口硬揉 `senderId`。
- 战局整包：`BattleClientHandler` 与 `BattleServerHandler` 自行 Bind/Unbind，方法与事件一一对应，发送方法封装填包。后续任何玩法模块同样：一对类型，禁止一条命令一个类。
- 热路径去掉「object 列表 + 二次查绑定表 + 每次快照数组」；槽内存已绑定调用。

**Non-Goals:**

- 合并或改造 `EventBus`。
- 世界快照、输入同步、Listen-Server。
- 代码生成 `NetEvent`↔`T` 的强制校验（Bind 写死对应即可；错误配对可作为调试断言，非本阶段合同）。
- 改 `.proto` 或传输宽限算法。

## Decisions

### Decision 1: 登记 Action，不登记 Handler 实例

`Register<T>(NetEvent eventId, Action<T> callback)`（客户端）与 `RegisterServer<T>(NetEvent eventId, Action<uint, T> callback)`（服务端）。注销仅为 `Unregister(eventId, callback)` / `UnregisterServer(eventId, callback)`。

模块 `Bind()` 内对每个事件调用 `Register(id, OnXxx)`。`Unbind()` 对每个已登记事件调用对应的 `Unregister(id, OnXxx)`。

委托必须是方法组（匿名 lambda 无法可靠退订）。编辑器可保留断言。

备选：继续 `INetHandler<T>` 实例键。否决：与整包多事件冲突。

备选：`Unregister(callback)` 反向扫全部事件。否决：账本复杂，调用方 Bind 时已知事件标识。

### Decision 2: 只保留正向表

仅 `NetEvent →` 已绑定调用列表（客户端与服务端分表）。模块 Unbind 自行对每个事件 `Unregister(eventId, OnXxx)`。

分发：直接遍历当前列表并调用。

不以 `object` Handler 实例为槽元素。

### Decision 3: 拆掉按实例的 Handler 接口主路径

删除玩法对 `INetHandler<T>` / `IServerNetHandler<T>` 的依赖作为登记方式。传输内部 Ping/Pong/心跳改为向同一总线 `Register` 方法组。若接口文件暂留，MUST NOT 再作为 `NetClient`/`NetServer` 对外主 API。

`GameJoinRequestHandler` 主路径删除：逻辑并入 `BattleServerHandler` 的 `OnGameJoinRequest`，内部仍调 `BattleSession.TryJoin` / `RejectJoin`。

后续聊天、输入、快照等若作为独立功能模块，各自一对 Client/Server Handler；若仍属战局模块，则只在 `Battle*Handler` 上加 `OnXxx`/`SendXxx`。禁止再引入 `INetHandler<某条消息>` 实现类作为玩法入口。测试程序集内的计数回调替身除外。

### Decision 4: 两套 Handler 类型，发送仍走窄门面

```
BattleServerHandler --Bind--> NetServer 登记口
        | OnGameJoinRequest --> BattleSession
        | Send 加入结果 --> IMatchMessenger

BattleClientHandler --Bind--> NetClient 登记口
        | OnGameJoinResponse
        | SendGameJoinRequest --> 客户端可靠发送口（不 Get NetServer）
```

二者零继承。本阶段客户端 Handler 至少登记 `GAME_JOIN_RESPONSE` 并提供发送加入请求封装，以便与整包模型对称；可不实现完整联机闭环（无房间匹配）。

## Risks / Trade-offs

- [委托相等依赖方法组] → 文档与编辑器断言禁止 lambda；测试用具名方法。
- [EventID 与 `T` 仍可能配错] → Bind 集中书写；可选调试日志，不做代码生成。
- [BREAKING 登记 API] → 同步改 `NetClient`/`NetServer` 内部与 `MessageProcessorTests`、`BattleSessionTests`。
- [分发中注销] → 直接改列表；靠后尚未调用的回调本趟可能不再执行。

## Migration Plan

先改分发器与测试，再改传输内部登记，再换战局 Handler，最后删除单协议 Handler 主路径。无运行时存档，无需数据迁移。回滚即恢复实例表 API。

## Open Questions

（无。延迟未知项不改变合同或任务切分。）
