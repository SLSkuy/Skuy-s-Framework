# 代码风格（C#）

详细风格规则。行为规则见 [AGENTS.md](../../AGENTS.md)。

使用 C#，4 空格缩进，大括号另起一行。**全仓库行尾为 CRLF（`\r\n`）** —— 新建和编辑的文本文件（`.cs`、`.md`、`.json`、`.asmdef`、`.txt`、shader 等）保持 CRLF，不要转成 LF。建议用含 `* text=auto eol=crlf` 的 `.gitattributes` 全仓库强制。公开类型与方法用 `PascalCase`；局部变量与参数用 `camelCase`。代码库使用中文 XML 文档摘要与行内注释 —— 扩展现有文件时保持一致，且**不要随意删除既有注释**。重构时原地保留并更新注释；只删除明显失效的内容，并让注释横幅（如 `// ========== 心跳 ==========`）与其字段组保持绑定。

**注释保留，但死代码删除。** 重构时直接删除旧实现 —— 不要留作注释、`#if false` 包裹或闲置。保持文件整洁，让新代码独立存在。只有被明确要求保留兼容时才保留旧代码，并清楚标注接缝（如 `// 旧实现，保留兼容` 横幅）使意图可见。

## 类型

- **类/结构体/枚举：** `PascalCase`。文件名与主类型名一致；一个文件一个主类型。
- **接口：** `I` 前缀 —— `IState`、`ISubSystem`、`IUIController`、`IClientTransport`、`IDataProxy`。
- **抽象基类：** `Base` 后缀 —— `SubSystemBase`、`EnumStateBase`、`EntityBaseState`、`ExtendableStateBase`。泛型基类保留类型参数 —— `MonoSingleton<T>`、`UIController<T>`、`EnumStateBase<TEnum>`。
- **管理器/协调器：** `Manager` 后缀 —— `GameStateManager`、`UIManager`、`ResourceManager`、`DataProxyManager`、`SystemManager`。
- **静态工具：** `Utils` 后缀，`static class` —— `MathUtils`、`GridUtils`、`NetUtils`、`TransformUtils`。
- **具体状态：** `State` 后缀 —— `EntityIdleState`、`LoadingState`、`GamingState`。
- **网络组件：** 使用既有的网络向名称，如 `NetworkObjectIdentity` 与 `NetworkTransformCapability`；客户端/服务端入口仍为 `NetClient` / `NetServer`。

## 字段与属性

代码库区分私有后备字段与序列化/公开字段：

- **私有字段：** `_camelCase` —— `_config`、`_context`、`_instance`、`_clientId`、`_fsm`、`_lastRtt`。私有静态字段也用 `_camelCase`（`_applicationIsQuitting`）。
- **`[SerializeField] private` 字段：** `camelCase`，**无**下划线 —— `animIn`、`uiControllerID`、`isVisible`、`entityId`、`role`。（去掉下划线以保持 Inspector 名称干净。）
- **公开字段：** `camelCase` —— `public bool tickDrive;`。涉及逻辑时优先用属性而非公开字段。
- **`protected readonly` 字段：** `PascalCase` —— `protected readonly EntityContext Context;`。
- **私有 `static readonly` 集合/锁：** `PascalCase` —— `Lock`、`SubSystems`。
- **属性：** `PascalCase` —— `Instance`、`Priority`、`CurrentState`、`EntityId`、`RTT`、`IsRunning`。简单 getter 优先用表达式体（`public uint EntityId => entityId;`）。

## 方法

- **公开与 protected virtual：** `PascalCase` —— `Move`、`Show`、`ChangeState`、`Init`、`Destroy`、`OnShow`、`Tick`、`CheckStateChange`。
- **Unity 生命周期：** `Awake`、`Start`、`Update`、`FixedUpdate`、`LateUpdate`、`OnEnable`、`OnDisable`、`OnDestroy`、`OnAnimatorMove`。
- **框架内部不可重写钩子：** 下划线前缀 —— `_Init()`、`_Destroy()`（在 `SubSystemBase` 上声明为非虚）。不要随意新增下划线前缀的公开 API；把它保留给基类生命周期接缝。
- **异步方法：** `Async` 后缀 —— `LoadResourceAsync`、`LoadAsync`。
- **多行参数列表：** 声明或调用一行放不下时，在行宽上限处换行，每行尽量多放参数 —— **不要**每个参数独占一行。续行缩进一级（4 空格）或对齐到左括号后，跟随所在文件风格。

  ```csharp
  // 不推荐：每个参数独占一行
  public static void Step(
      IEntitySimulation simulation,
      EntityInputCommandBuilder commandBuilder,
      uint tick,
      float deltaTime,
      in InputState input)

  // 推荐：到达行宽上限再换行，一行尽量多放参数
  public static void Step(IEntitySimulation simulation, EntityInputCommandBuilder commandBuilder,
      uint tick, float deltaTime, in InputState input)
  ```

## 成员声明顺序

类或结构体内，成员自上而下按固定顺序声明。注明处用 `#region` 包围，并在一个组内把功能相关的字段放一起。

1. **序列化字段** —— 所有 `[SerializeField]`（以及任何 Inspector 可见的 `public`）字段在最前，用 `[Header]` / `[Tooltip]` 分组与描述。`camelCase` 无下划线。
2. **类类型引用字段** —— 对其他类/实例类型的私有引用，如 `_config`、`_context`、`_fsm`、`_messageProcessor`（`_camelCase`）。
3. **基础值类型字段** —— `int`、`float`、`bool`、枚举等，如 `_clientId`、`_token`、`_tryReconnect`。把服务同一功能的字段放一起；用注释横幅（如 `// ========== 网络心跳 ==========`，见 `NetClient`）标注子组是既有做法。
4. **属性** —— 用 `#region 属性` 包围。简单 getter 优先用表达式体（`public uint EntityId => entityId;`）。
5. **事件** —— 用 `#region 事件` 包围（`public event Action<T> ...`、`event Action<IUIController>`）。
6. **方法** —— 按顺序声明，公开 API 在前，私有 helper 在后。**不要用 `#region` 包围方法或方法组**；`#region` 仅限 `属性`、`事件`、`生命周期`、`子系统生命周期`（见第 7 条）。
7. **生命周期方法** —— 用生命周期 `#region` 包围并放在**文件末尾**。按类的基类选用对应 region：
   - **MonoBehaviour / Unity 生命周期**（`Awake`、`Start`、`OnEnable`、`OnDisable`、`Update`、`FixedUpdate`、`LateUpdate`、`OnDestroy`、`OnAnimatorMove` 等）→ `#region 生命周期`（或 `#region Unity生命周期`）。
   - **`SubSystemBase` 重写**（`Init`、`Destroy`、`BindEvents`、`Update`、`FixedUpdate`、`LateUpdate`）→ `#region 子系统生命周期`（或 `#region SubSystem生命周期`）。一个类最多用这两个 region 之一 —— `SubSystemBase` 是纯 C# 类而非 MonoBehaviour，其重写由框架驱动，非 Unity 驱动。

参考实现：`NetworkObjectIdentity.cs`（序列化字段 → `#region 属性` → `#region 事件` → 方法 → `#region 生命周期`）是应遵循的范本。`EntityCharacter.cs` 仍带遗留的功能命名 region（`#region 实体控制`、`#region 模拟入口`）；新代码不要复刻 —— 只有它的 `#region 属性` 符合现行规则。

## 文件组织

- 一个文件一个主类型；文件名与类型一致。
- 应用上述成员顺序；不要把字段、属性、方法交错乱序。**只用四个 `#region` 标签：** `#region 属性`、`#region 事件`、`#region 生命周期`、`#region 子系统生命周期`。MonoBehaviour 的 Unity 生命周期方法用 `#region 生命周期`，`SubSystemBase` 重写用 `#region 子系统生命周期`；一个类最多用其一，放在文件末尾。不要给普通方法或其他成员组加 `#region`。编辑已含多余功能命名 region（如 `#region 实体控制`、`#region 模拟入口`）的遗留文件时，保留原样、不删除，但不要新增。
- 对公开类型、公开方法、非显然的 protected virtual 加 `/// <summary>` XML 文档（中文可以）。
- Inspector 字段用 Unity 特性：`[Header]`、`[Tooltip]`、`[SerializeField]`；依赖兄弟组件或必须唯一的 MonoBehaviour 用 `[RequireComponent]` / `[DisallowMultipleComponent]`。
- `var` 与显式类型跟随所在文件；两者都有出现。文件已用表达式体的地方保持表达式体。
