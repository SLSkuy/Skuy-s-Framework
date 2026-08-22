# 架构

本文件描述项目结构与需要遵循的既有模式，属于参考资料 —— 行为规则见 [AGENTS.md](../../AGENTS.md)。

## 项目结构与模块组织

这是一个 Unity 项目。运行时代码位于 `Assets/Scripts`，场景入口 `Launch.cs` 和 `MainEntry.cs` 直接放在 `Assets/Scripts/` 下（均为全局命名空间，无 `namespace` 声明）。运行时代码可按职责拆分为多个 `.asmdef` 程序集。保持显式依赖方向，让纯 simulation/core 程序集独立于 Unity 场景胶水、gameplay 宿主、transport 与测试程序集。

`Assets/Scripts/` 下的顶层模块：

- `Framework/` —— 可复用核心框架。`Common/`（单例、状态机、子系统基类）、`SubSystems/`（Camera、DataProxy、ObjectPool、Resource、SceneControl、States、Timer、UI）、`Input/`、`Navigation/`（基于 ECS 的 A\*）、`Event/`。
- `GamePlay/` —— gameplay 代码。`EntitySystem/`（实体宿主、模块、控制器与 simulation 适配器）、`EntitySimulationCore/`（纯 command/state/simulation 契约与核心 Tick 工具）、`NetworkSync/`（Config、Runtime capabilities、Snapshot 插值与 TestSimulator 驱动）、`Protocol/Generated/`（Protobuf 生成，勿手改）、`Proxy/`。
- `Network/` —— 网络。`Client/`、`Server/`、`Transport/`（`Kcp/`、`Tcp/`）、`Config/`、`Interface/`、`Protocol/`。
- `Events/` —— 跨模块事件枚举（如 `NetEvent`）。
- `Utils/` —— 无状态静态工具（`MathUtils`、`GridUtils`、`NetUtils`、`TransformUtils`，以及 `DataStruct/KDTree`）。
- `Editor/` —— 仅编辑器工具（`Protobuf/`、`UIFramework/`）。编辑器脚本必须放在 `Editor/` 文件夹下，以便 Unity 在构建时排除。
- `Core/`、`Debug/` —— 辅助运行时工具。
- `Tests/` —— 目前存放运行时调试面板（如 `NetworkTestPanel.cs`）；新的 EditMode/PlayMode 单元测试放在此处或对应模块旁。

场景位于 `Assets/Scenes`，开发场景在 `Assets/Scenes/Dev` 下。生成的协议类位于 `Assets/Scripts/GamePlay/Protocol/Generated`；除非生成器不可用，否则不要手改生成文件。第三方代码放在 `Assets/ThirdParty`。

> **注意：** 下方模块清单、命名空间表与具体类名会随代码库演进而漂移。行动前始终以实际代码为准 —— 见 [AGENTS.md](../../AGENTS.md) 的「仓库即事实」。

## 命名空间约定

命名空间是逻辑模块名 —— **不**严格镜像文件夹路径。使用既有的命名：

| 命名空间                 | 用途                                                   | 示例                                                                     |
| ------------------------ | ------------------------------------------------------ | ------------------------------------------------------------------------ |
| `Framework`              | 核心框架服务、单例、子系统基类、`Global` 定位器         | `Global.cs`、`MonoSingleton.cs`、`SubSystemBase.cs`、`GameStateManager.cs` |
| `Framework.Core`         | UI 控制器基类与共享 UI 核心                            | `UIController.cs`                                                        |
| `Framework.StateMachine` | 通用状态机（`IState`、`EnumStateBase<T>`）             | `IState.cs`、`EnumStateBase.cs`                                          |
| `Network`                | 客户端/服务端网络、transport、消息处理                 | `NetClient.cs`、`NetServer.cs`                                           |
| `Events`                 | 跨模块事件枚举                                         | `NetEvent.cs`                                                            |
| `EventProcess`           | 事件总线（`EventBus.Get<T>()`）                        | —                                                                        |
| `GamePlay.EntitySystem`  | 实体角色、FSM 状态、网络身份                           | `EntityCharacter.cs`、`EntityBaseState.cs`、`NetworkObjectIdentity.cs`   |
| `GamePlay.NetSync`       | 网络能力、复制、预测与插值                             | `EntityReplicationSystem.cs`、`NetworkTransformCapability.cs`            |
| `Utils`                  | 无状态静态工具                                         | `MathUtils.cs`                                                           |
| `NetConnect`             | 底层连接原语                                           | —                                                                        |
| *(全局)*                 | 仅场景入口                                             | `Launch.cs`、`MainEntry.cs`                                              |

只有 `Launch` 和 `MainEntry` 可以位于全局命名空间；其他都必须声明模块命名空间。新增子系统时，复用所属模块的命名空间，不要另起新的。

## 架构与模式

编写面向框架的代码前，先匹配这些既有模式：

- **服务定位器 + 子系统。** 框架服务继承 `SubSystemBase` 并自注册到静态 `Global` 定位器（见 `Framework/Common/SubSystemManager/`）。业务代码通过 `Global.Get<T>()` / `Global.TryGet<T>(out var s)` 获取。生命周期由不可重写的 `_Init()` / `_Destroy()` 钩子驱动；子类改为重写虚方法 `Init()` / `Destroy()` / `BindEvents()` / `Update()` / `FixedUpdate()` / `LateUpdate()`。每个子系统暴露唯一的 `SubSystemPriority`。
- **MonoBehaviour 单例。** 继承 `MonoSingleton<T>`；通过 `Instance` 访问，重写 `Init()` / `Destroy()`，并在应用退出时调用 `ShutDown()`。
- **状态机。** 状态实现 `IState`（或继承 `EnumStateBase<TEnum>` / `ExtendableStateBase`）。`Update()` 调用 `Tick()` 再调用 `CheckStateChange()`；重写这两个而非 `Update`。具体状态用 `...State` 后缀（如 `EntityIdleState`、`MainMenuState`）。
- **事件总线。** 用 `EventBus.Get<TEvent>().Dispatch(data)` 分发跨模块事件；类内直接订阅用 `event Action<T>`。
- **数据代理。** `IDataProxy` 实例通过 `DataProxyManager` 注册，经 `Global.GetDataProxy<T>()` / `Global.TryGetDataProxy<T>()` 访问。
- **网络 transport。** `NetClient` 与 `NetServer` 均为 `SubSystemBase`；transport 实现 `IClientTransport` / `IServerTransport`，KCP 与 TCP 变体在 `Network/Transport/` 下。消息用 Google.Protobuf；生成消息位于 `GamePlay/Protocol/Generated/`。
- **保持 MonoBehaviour 场景胶水与可复用服务分离。** MonoBehaviour（`EntityCharacter`、`UIController`、`NetworkObjectIdentity`）把 Unity 生命周期接到框架服务；它们不应包含属于 `SubSystemBase` 或纯核心程序集的可复用逻辑。
- **新代码迁移规则。** 某模块有了批准的新职责文件夹/程序集后，新代码只放那里。不要在遗留的 `EntitySystem/Sync`、控制器角色实现或兼容文件里扩展新的同步行为。兼容垫片必须是临时的、有文档记录、并由一个迁移/移除任务跟踪。
