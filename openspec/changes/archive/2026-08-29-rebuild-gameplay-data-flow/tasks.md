## 1. 消息分发

- [x] 1.1 将 `MessageProcessor` 改为按事件保存处理器实例列表，支持同一事件多个 Handler，并提供按实例注销；用两个测试 Handler 同事件均收到、注销其一后只有另一个收到、注销未登记实例为无操作来验证
- [x] 1.2 引入客户端 `INetHandler<T>` 与服务端 `IServerNetHandler<T>`，`NetClient`/`NetServer` 改为登记实例而非覆盖式 `Action`；用编译期不再存在「按 event 覆盖唯一回调」的登记签名来验证
- [x] 1.3 将 Ping/Pong/心跳/可靠连接响应改为内部 Handler 实例并走同一列表分发，且不进入玩法模块；用传输测试仍能握手与心跳、战局名册不因心跳变化来验证
- [x] 1.4 从 `Global` 移除按 `NetEvent` 整槽注销（及覆盖式登记）快捷方式；用解决方案内无 `Global.UnRegNetHandler(eventId)` 调用来验证

## 2. 拆除联机玩法层

- [x] 2.1 删除 `MultiPlayManager`、`ServerSimulationHost`、`ClientSimulationHost`、`MatchRoom`、`ServerHandler`、`ClientHandler` 及以其为入口的运行时引用；用解决方案编译不再解析这些类型来验证
- [x] 2.2 删除 `CharacterReplicationSystem` 运行时路径及其作为世界循环的接线（含仅被该路径使用的预测/插值入口）；用玩法程序集中不再存在仍被子系统管理器驱动的复制主循环来验证
- [x] 2.3 删除 `SyncTestPanel` 与快照同步 EditMode 测试；用测试运行器中不再出现这些用例、场景中不再依赖该面板开服开客来验证

## 3. 传输门面与战局名册

- [x] 3.1 实现 `IConnectionEvents` 与 `NetServer` 适配器，将连接彻底移除翻译为门面事件；用适配器触发 Removed 且订阅方不引用 `NetServer` 类型来验证
- [x] 3.2 实现 `IMatchMessenger` 与 `NetServer` 适配器（可靠单发/广播），战局与 Handler 只依赖该接口；用假门面记录发送、战局代码无 `NetServer.Send*` 调用来验证
- [x] 3.3 实现 `BattleSession` 与 `BattlePlayer`：独立 `playerId`、按连接加入/拒绝重复加入、容量上限；用加入后 `playerId != clientId` 可成立、二次加入失败来验证
- [x] 3.4 将门面 `Removed` 接到战局：已加入则玩家离开，未加入则名册不变；用已加入连接移除后名册无该玩家、未加入移除名册不变、战局类不读取 `ClientManager` 来验证
- [x] 3.5 加入成功不 Spawn、不广播世界快照、不入队 `Player_Input`；用加入后场景无新 Authority 角色、无快照广播调用来验证

## 4. 玩法会话编排

- [x] 4.1 实现 `GameManager` 相位 `Idle/InPlay/Ending`，`StartLocal`/`Stop` 驱动 `LocalSimulationHost`，不拉起网络；用开始后单机运行且 `NetClient`/`NetServer` 未因该调用 Start、进行中再次 Start 失败来验证
- [x] 4.2 在 `GameCore` 注册 `GameManager`；`LocalSimulationHost` 去掉对已删联机 Manager 的互斥；用进程启动后可取到编排器、单机启停只经编排器来验证
- [x] 4.3 确认 `GameStateManager` 暂停不停止玩法会话；用会话进行中切暂停后 Host 仍视为运行、仅 `Stop` 才拆角色来验证
- [x] 4.4 `LocalPlayTestPanel` 改为只请求 `GameManager`；用面板开始/停止仍能附身与销毁本地角色来验证

## 5. 回归

- [x] 5.1 等待 Unity 编译通过，控制台无因本 change 新增的 Error；用编辑器编译状态或 `read_console` 验证
- [x] 5.2 运行相关 EditMode/PlayMode 测试（分发、战局名册、单机编排）并通过；用测试运行器结果验证
