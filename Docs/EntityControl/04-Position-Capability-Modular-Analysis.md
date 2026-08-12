# 位移能力模块化分析

## 结论

当前位移链路已经具备做“配置驱动能力组件 + 运行时自动装配同步组件”的基础。

现有实现里，真正负责位移逻辑的是 `MovementModule`，`EntityCharacter` 只是把输入写入实体上下文，`NetPositionSync` 只负责把位移结果同步出去。这个结构本身已经接近模块化设计，不需要推倒重来，更适合做“收敛职责 + 映射装配”。

## 当前位移链路

### 1. 输入层

`PlayerController` / `AuthorityController` / `ReplicaController` 负责不同角色的输入和同步调度。

### 2. 实体意图层

`EntityCharacter` 负责接收：

- `Move`
- `Aim`
- `Jump`
- `StartSprint`
- `StopSprint`
- `ToggleRun`

它本身不直接执行位移计算，而是把意图写入 `EntityContext`。

### 3. 位移能力层

`MovementModule` 才是当前的位移核心，已经包含：

- 移动方向计算
- 重力
- 跳跃
- 冲刺
- 视角朝向
- 模型朝向
- root motion 位移

这说明“位移”已经不是控制器内的杂糅逻辑，而是可独立承载的能力模块。

### 4. 状态编排层

`EntityLocomotionState` 负责在每帧里组织：

- `Rotate`
- `UpdateMeshFacing`
- `Move`

也就是说，状态层决定“什么时候驱动位移”，`MovementModule` 决定“怎么移动”。

### 5. 同步层

`NetPositionSync` 只同步：

- `Position`
- `Velocity`
- `MovementState`

当前已经不再同步旋转，这进一步说明“位移”与“网络同步”已经可以分离成两个模块。

## 这个方案为什么可行

### 可行点一：能力已经是模块化的

`MovementModule` 本身就是一个能力模块，而不是散落在多个控制器里的逻辑集合。

### 可行点二：同步对象是能力结果，不是能力本身

网络层不需要知道“如何跳跃”或“如何计算重力”，只需要知道：

- 这个实体有没有位移能力
- 这个位移能力对应哪个同步模块
- 当前角色要不要驱动它

### 可行点三：配置能表达“意图”

如果 `EntityCharacter` 持有一个能力配置，运行时就可以从配置中知道：

- 是否启用位移能力
- 是否启用动画能力
- 是否启用受击能力
- 是否启用技能能力

再由 `NetEntitySyncRoot` 根据能力清单自动挂载对应同步模块。

## 推荐拆分方式

### EntityCharacter 负责“实体能力装配”

`EntityCharacter` 更适合成为实体能力宿主，而不是位移逻辑容器。

它可以负责：

- 读取实体能力配置
- 按需添加能力组件
- 暴露基础意图接口
- 持有实体运行上下文

### MovementModule 负责“位移能力实现”

位移模块继续承担：

- 移动
- 跳跃
- 冲刺
- 重力
- 朝向
- root motion

它不应知道网络角色，也不应直接关心控制器类型。

### NetEntitySyncRoot 负责“位移同步能力装配”

如果实体配置里启用了位移能力，Root 可以自动装配：

- `NetPositionSync`

未来如果位移继续细分，还可以扩展为：

- `NetPositionSync`
- `NetRotationSync`
- `NetMotionStateSync`

## 与配置驱动的关系

建议把“能力配置”和“同步映射”分开：

### 能力配置

描述实体本体应该具备什么：

- 位移
- 动画
- 技能
- 交互
- 受击

### 同步映射

描述网络层要同步什么：

- 位移能力 -> `NetPositionSync`
- 动画能力 -> `NetAnimationSync`
- 技能能力 -> `NetSkillSync`

这样做的好处是：

- 配置只表达业务意图
- Root 只负责装配
- 同步模块只负责同步

## 当前位移组件的定位

### `EntityCharacter`

当前仍然是“意图入口 + 状态机装配者”。

适合继续保留，但后续应该从“直接初始化位移和动画模块”走向“按能力配置装配模块”。

### `MovementModule`

这是最适合优先配置化的对象。

因为它已经具备独立边界，且和网络同步的耦合很弱。

### `NetPositionSync`

这是“位移结果同步模块”，不是“位移能力模块”。

它应当在位移能力存在时自动出现，而不是由 Controller 硬编码要求。

## 迁移建议

### 第一阶段

- 保留现有 `MovementModule`。
- 让 `EntityCharacter` 继续负责现有输入写入和状态驱动。
- 把“是否启用位移能力”抽到配置中。
- 让 `NetEntitySyncRoot` 从能力配置中识别需要挂载的同步组件。

### 第二阶段

- 将 `EntityCharacter` 的初始化改为能力装配驱动。
- 把位移、动画等模块的创建逻辑从 `Start()` 中拆出去。
- 让 Root 在角色切换后统一完成同步组件配置。

### 第三阶段

- 将能力配置扩展到技能、交互、受击、动画事件等系统。
- 形成“能力组件 -> 同步组件 -> Controller”三层映射。

## 风险点

- 如果能力配置和实际场景组件不一致，会导致运行时装配失败。
- 如果位移能力仍然直接依赖 `EntityCharacter.Start()`，后续扩展会再次分裂。
- 如果 `NetEntitySyncRoot` 同时承担太多具体业务，容易变成新的大杂烩。

## 建议结论

位移模块是当前最适合作为“配置驱动能力装配”试点的对象。

原因很简单：

- 已经模块化
- 业务边界清晰
- 同步边界清晰
- 对后续技能系统的形态最有参考价值

因此，下一步最合理的方向不是重写位移，而是：

1. 把位移能力定义成可配置能力。
2. 让 `EntityCharacter` 按配置装配能力组件。
3. 让 `NetEntitySyncRoot` 按能力清单自动装配同步组件。
4. 继续保持 Controller 只负责角色驱动，不直接绑定具体同步实现。
