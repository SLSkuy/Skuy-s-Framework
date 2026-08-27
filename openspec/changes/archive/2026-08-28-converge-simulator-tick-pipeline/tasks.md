## 1. 注册槽收敛来源与命令构建

- [x] 1.1 将 `RegisteredEntity` 改为 class：持有 Identity、Character、`EntityCommandBuilder`、可空 `IInputStateProvider`；提供 `CollectIntent`（null 来源 → 空快照）、`Build`、`SetInputSource`。用连续两次 Build 边沿正确、来源 null 时 Collect 为空来验证
- [x] 1.2 更新 `EntityRegistry` 存储 class 引用。用重复 id 拒绝、注销后 `TryGet` 失败来验证
- [x] 1.3 从 `Simulator` 删除 `_commandBuilders`、`_deviceInput`、`_possessedEntityId`、`SetDeviceInput` 及附身采样。用 Simulator 源文件不再引用设备提供器字段来验证

## 2. Tick 先收集再模拟，删除邮箱

- [x] 2.1 删除 `CommandMailbox` 与 `Simulator.SubmitInput`。用解决方案无这些符号且编译通过来验证
- [x] 2.2 Tick 回调先对已初始化可步进槽位 `CollectIntent` 写入复用字典，全部收集完再 `Build` + `Step`。核内无收集失败分支、不因缺采样跳过时钟。用未设置来源时时钟仍推进且该实体按空输入步进来验证
- [x] 2.3 新增 `NetworkInputProvider`（`GamePlay.Simulator`）：实现现有 `IInputStateProvider.GetInputState()`，无采样返回 `default`；入队方法不放进接口。用将其挂到槽位后缺采样实体仍步进、收集段对 `LocalInputProvider` 与该类型无种类分支来验证。**不要**改 `EntityInputBuffer` 窗口语义及其内部 builder，**不要**改 `IInputStateProvider`

## 3. 单机 Host 绑定来源

- [x] 3.1 `LocalSimulationHost` 在 Register 后把 `GameCore.LocalInput` 设到该角色槽位；Stop 时来源置空并注销。不再调用已删的核内 `SetDeviceInput`/`Possess` 采样。用持续移动意图下每拍推进、Stop 后实例移除来验证
- [x] 3.2 重启会话后边沿不继承上一角色。用停止后再开单机，第一拍按下不会被当成「一直按住」来验证

## 4. 编译与可玩验证

- [x] 4.1 等待 Unity 编译结束，控制台无本 change 引入的 Error。用编辑器编译状态或 Console 验证
- [x] 4.2 开发场景单机 Start / 移动 / Stop / 再 Start 可玩。用 Play 模式验证
