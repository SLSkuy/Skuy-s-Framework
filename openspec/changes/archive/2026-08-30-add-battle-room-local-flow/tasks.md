## 1. 房间与战局管理器

- [x] 1.1 实现 `GameRoom` 成员表：独立 `playerId`、容量、房主；用加入后 `playerId` 允许不等于连接标识、二次加入同一连接失败来验证
- [x] 1.2 实现 `BattleManager`：创建/解散至多一间活动房间，`Admit(connectionId)` 与 `AdmitLocal` 写入名册；用第二间活动房被拒绝、进房不 Spawn 来验证
- [x] 1.3 本机离开与解散走同一名册 API，不查询 `NetServer`/`ClientManager`；用本机离开后名册无该成员、未加入连接离开名册不变来验证
- [x] 1.4 将现有 `BattleSession` 名册职责下沉或内聚进房间/战局管理器，避免平行表；用解决方案中单机路径不再绕过战局管理器维护第二套 `playerId` 表来验证

## 2. 流程编排与单机 Host

- [x] 2.1 将 `GameManager` 相位改为 `Idle/InLevel/ChangingLevel`；`StartLocalPlay` 先建房本机进员再经 `SystemManager` 启动 `LocalSimulationHost`；用无活动房间时不能把「仅 StartSession」当作正式开玩完成、开始后相位为 `InLevel` 来验证
- [x] 2.2 `StartLocalPlay` 不拉起 `NetClient`/`NetServer`；进行中再次开始失败且不创建第二间房、不启动第二套 Host；用网络栈未因该调用 Start、二次开始返回失败来验证
- [x] 2.3 实现 `ChangeLevel`：进入 `ChangingLevel`、停止 Host 会话、房间不解散、再 `StartSession` 回到 `InLevel`；用换关后房间仍活动且成员仍在、场景重新出现一个 LocalPlay 角色来验证
- [x] 2.4 实现 `StopPlay`：停 Host、解散房间、回 `Idle`；用 pawn 销毁、房间不再活动、可再次 `StartLocalPlay` 来验证

## 3. 注册与测试入口

- [x] 3.1 在 `GameCore` 注册 `BattleManager` 与 `GameManager`；用进程启动后可取到二者、测试面板不再 `RegisterSystem<GameManager>` 作为正式入口来验证
- [x] 3.2 `LocalPlayTestPanel` 只请求 `GameManager` 的开始/换关/结束，并展示房间活动、成员数、关卡相位、Host 是否运行；用面板走完开玩、换关、结束仍能附身与销毁本地角色来验证

## 4. 回归

- [x] 4.1 等待 Unity 编译通过，控制台无因本 change 新增的 Error；用编辑器编译状态或 `read_console` 验证
- [x] 4.2 运行相关 EditMode/PlayMode 测试（房间名册、流程编排、单机 Host）并通过；用测试运行器结果验证
