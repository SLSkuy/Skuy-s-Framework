## 1. 网络分发账本

- [x] 1.1 将 `MessageProcessor` 改为按 `NetEvent` 保存已绑定回调列表（客户端 `Action<T>`、服务端 `Action<uint, T>` 分表），正向分发不再以 Handler 实例为键；用同一事件登记两条方法组均收到、重复登记同一对只触发一次来验证
- [x] 1.2 实现 `Unregister(eventId, callback)`，未登记为无操作；用同事件删其一只剩另一条、注销未登记不抛错来验证
- [x] 1.3 分发直接遍历列表；用回调内注销尚未调用的另一条后本趟不再调用它来验证
- [x] 1.4 对外 API 改为登记/注销方法组，移除以 `object`/`INetHandler` 实例为键的主登记签名；用编译期不再存在 `Register(event, INetHandler<T>)` / `Unregister(object)` 作为 `NetClient`/`NetServer` 主 API 来验证

## 2. 传输内部改挂方法组

- [x] 2.1 `NetClient` / `NetServer` 将 Ping/Pong/心跳/可靠连接响应改为向分发器登记实例方法组，停用内部 `DelegateNetHandler` 实例表；用传输仍能处理这些协议、玩法测试不因心跳改名册来验证
- [x] 2.2 确认玩法 `Unbind` 路径不会注销传输已登记的心跳等方法组；用先挂心跳回调再拆除玩法模块后心跳仍被调用的测试来验证

## 3. 战局整包 Handler

- [x] 3.1 实现独立的 `BattleServerHandler`：`Bind`/`Unbind` 自行登记 `GAME_JOIN_REQUEST` 到 `OnGameJoinRequest`，内部调用 `BattleSession`，回包只经 `IMatchMessenger`；用登记后加入请求到达会改名册且假门面收到加入响应、类型不引用 `NetServer.Send*` 来验证
- [x] 3.2 实现独立的 `BattleClientHandler`：`Bind`/`Unbind` 登记 `GAME_JOIN_RESPONSE`，提供 `SendGameJoinRequest` 封装；注入客户端发送口而非 `Get<NetServer>()`；用仅绑定服务端 Handler 时客户端响应回调不被调用、发送封装会发出加入请求载荷来验证
- [x] 3.3 将加入路径测试从单协议 `GameJoinRequestHandler` 改接到服务端模块 Handler，并删除该单协议类型主路径；用解决方案不再把 `GameJoinRequestHandler` 作为运行时接收入口、原加入用例仍通过来验证
- [x] 3.4 确认玩法程序集中不再存在「单条 `NetEvent` / 单条协议一个业务 Handler 类型」；用 `GamePlay` 下无此类接收入口（`Tests` 替身除外）、战局仅 `BattleClientHandler` 与 `BattleServerHandler` 一对来验证

## 4. 回归

- [x] 4.1 等待 Unity 编译通过，控制台无因本 change 新增的 Error；用编辑器编译状态或 `read_console` 验证
- [x] 4.2 运行分发与战局相关 EditMode 测试并通过；用测试运行器结果验证
