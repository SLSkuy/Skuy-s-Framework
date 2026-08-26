## 1. 模拟核骨架

- [x] 1.1 补全 `Simulator` 子系统：`Init` 用 `SimulationConfig` 创建并启动唯一 `TickSystem`，`Update` 驱动时钟，`Destroy` 停止时钟；用 Unity 编译通过且控制台无该文件相关错误来验证
- [x] 1.2 实现实体注册表：非零 `entityId` 注册/注销、拒绝重复 id、保存 Identity 与 `EntityCharacter`；用重复注册失败且原实例仍可查询来验证
- [x] 1.3 实现按 `entityId + tick` 的命令邮箱：`SubmitInput`、本拍消费、缺失时返回空 `InputState`；用写入序号 N 后步进 N 能取到该快照、未写入则空快照来验证
- [x] 1.4 每实体持有迁入 Simulator 后的命令构建器，Dispatch 在每个 Tick 对已注册实体执行「取邮箱 → Build → `EntityCharacter.Step`」；用已 Init 的测试实体在 Tick 后位移相对空闲有变化来验证
- [x] 1.5 实现附身 API（至多一个 possessed id）：仅对被附身实体从 `IInputStateProvider` 采样并 `SubmitInput`；用未附身实体本拍邮箱不被设备输入覆盖来验证
- [x] 1.6 实现无网络的 `Capture`：对已注册实体返回 `CaptureRollbackState`；用步进后再 Capture 的位置与实体当前模拟一致来验证

## 2. 迁入共享实现并替换预制体

- [x] 2.1 将 `EntityCommandBuilder` 迁到 `GamePlay.Simulator` 命令目录，删除 EntitySystem 旧文件，更新全部引用；用解决方案内无旧命名空间残留且编译通过来验证
- [x] 2.2 将 `CharacterInputBuffer` 迁到 `GamePlay.Simulator` 命令目录，删除 MultiPlay 旧文件，复制系统改为引用新类型；用权威输入入队与消费仍通过同一类型来验证
- [x] 2.3 将 `PlayerSpawner` 迁到 `Simulator/Spawn`，删除 MultiPlay 旧文件；`ClientSimulator`/`ServerSimulator` 改调新 API 并在联机生成后显式 `Replication.Register`；用开服生成角色仍能被复制系统登记来验证
- [x] 2.4 实现 Simulator Spawn（或扩展迁入的生成器）：`EntityCharacter` + `EntityObjectIdentity`、失败销毁半成品；用无效原型不留下注册项来验证
- [x] 2.5 更新 `NetPlayer`：去掉 `PlayerController`，将 `NetworkObjectIdentity` 换为 `EntityObjectIdentity`；用预制体 YAML/检视器不再引用已删类型来验证

## 3. LocalHost 会话

- [x] 3.1 实现 `LocalSimulationHost`：`StartSession` 注册 `Simulator`、分配非零本地 id、Spawn(LocalPlay)、Possess；不启动 `NetClient`/`NetServer`。用会话启动后场景存在一个 LocalPlay 角色且网络栈未因该调用启动来验证
- [x] 3.2 每个 Simulator Tick 从 `GameCore.LocalInput` 采样写入被附身邮箱；用持续移动意图下角色每拍被推进来验证
- [x] 3.3 Possess 成功后绑定相机到 `orientation`（若存在）并锁定光标；Stop 时解锁光标。用会话启动后活动相机跟随视角节点来验证
- [x] 3.4 `StopSession` 停止步进、注销并销毁本会话角色、解除附身，之后可再次 `StartSession`；用停止后实例消失且重启再生成本地角色来验证

## 4. 删除 Controller 与死代码

- [x] 4.1 删除 `PlayerController`、`AIController`、`EntityControllerBase` 及 Controllers 目录；复制系统去掉 Controller 字段，`PredictOwnedTick` 改为采样 `LocalInputProvider`；用无 `PlayerController` 符号且客户端预测仍能发出输入来验证
- [x] 4.2 删除 `RegisterLocalPlay`、`SimulateLocalPlayTick` 及 LocalPlay 步进分支；用复制系统源文件不再包含这些 API 来验证
- [x] 4.3 删除 `NetworkObjectIdentity`、`EntitySimulationMode`、重复的 `IEntityObjectIdentity`（MultiPlay 与 EntitySystem 副本）；用仅剩 `GamePlay.Simulator` 身份类型且编译通过来验证
- [x] 4.4 删除无引用的 `NetInputProvider`；若 `NetworkTimeSystem`/`SyncConfig` 无注册与无资源依赖则一并删除；用全局搜索无残留引用验证

## 5. 入口与互斥

- [x] 5.1 增加最小单机调试入口（优先在现有同步测试场景加「单机」按钮），可 Start/Stop 本地会话；用 Play 模式点击后能控制角色来验证
- [x] 5.2 面板层互斥：单机会话运行时禁用开服/开客户端，联机运行时禁用单机启动；用一侧运行时另一侧按钮不可用或调用失败来验证
- [x] 5.3 确认单机生成不注册进复制系统；用未开联机时复制系统计数不为该 pawn 增加来验证

## 6. 编译与回归

- [x] 6.1 脚本与预制体改动后等待 Unity 编译结束，控制台无新增 Error（含丢失脚本）；用 `read_console` 或编辑器编译状态验证
- [x] 6.2 联机 SyncTest 开服/开客户端仍可启动，预测输入不再依赖已删控制器；用测试面板仍能切到 Server/Client 且本地角色可动来验证
