## 1. 模拟核（对齐单机 Tick 结构）

- [x] 1.1 在 `DispatchTick` 中跳过 Replica 的收集与 `Step`，LocalPlay/Authority 行为不变；用注册一个 Replica 推进时钟后其位移不因空命令改变、Authority 仍步进来验证
- [x] 1.2 增加 `TryRestore`，转调已初始化实体的 `RestoreRollbackState`；用 Capture 再 Restore 位姿一致来验证
- [x] 1.3 为权威槽增加缓冲门面：`Enqueue` 走 `EntityInputBuffer` 窗口，`GetInputState` 交出下一拍 `InputState`（缺则为空），边沿只留在 `RegisteredEntity`；用乱序/缺口入队后步进命令符合窗口规则来验证
- [x] 1.4 若发包需要对齐时钟，仅转发核上已有 Tick，禁止再 `new TickSystem`；用解决方案内联机 Host 只有一份追帧器来验证

## 2. MultiPlay：房间、编解码、适配器

- [x] 2.1 实现一场一房：Join 分配实体、Leave/断线 Unregister+Destroy、`TryAuthorize`；用未授权输入不入队、断线后核内无该 id 来验证
- [x] 2.2 将输入/快照字段对应抽到 MultiPlay Codec（逻辑来自现有转换，不依赖复制类）；用往返 `InputState`/`EntityRollbackState` 字段一致来验证
- [x] 2.3 实现收发适配：客发 `PLAYER_INPUT`，服校验后 Enqueue，服按 `snapshotTickRate` 广播 `WORLD_SNAPSHOT`，客按 tick 丢弃过期包；用两进程日志/面板见到成对收发来验证

## 3. Host 同构装配

- [x] 3.1 `ServerSimulationHost`：骨架同 `LocalSimulationHost`（Init 持核、Update 驱动、Stop 拆实体）；Join 后 Spawn Authority、绑缓冲门面、StartClock；用仅开服时核 `CurrentTick` 增加且不注册复制系统来验证
- [x] 3.2 `ClientSimulationHost`：同构持核；不 `SetInputSource(LocalInput)`；Tick 采样设备并 Send；快照 Spawn Replica 并 Restore；己方相机绑 `orientation`；用开客后本地摇杆不立刻移动、快照到达后位姿更新来验证
- [x] 3.3 `MultiPlayManager` 改为启动上述 Host 与 Net，删除对 `CharacterReplicationSystem` 的注册与 `ClientSimulator`/`ServerSimulator` 主路径；用联机运行时复制系统未驱动世界来验证
- [x] 3.4 停掉复制子系统自有 Tick 订阅（删除或掏空 `AdvanceTick` 注册），避免双模拟；用联机时角色只被一份 `Simulator.Step`（仅服 Authority）推进来验证

## 4. 入口与回归

- [x] 4.1 复用现有 `SyncTestPanel`：按钮仍开服/开客/停止并走 `MultiPlayManager` 新路径；状态改为模拟核而非复制系统；不新建联机面板。用该面板能开服、开客、停止且显示连接与模拟 Tick 来验证
- [x] 4.2 单机 LocalHost 路径回归：不联机时仍可 Start/Stop 本地角色；用 LocalPlay 面板可玩来验证
- [x] 4.3 等待 Unity 编译通过，控制台无新增 Error；用编辑器编译状态或 `read_console` 验证
