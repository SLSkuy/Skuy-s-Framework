# Network 网络层

本文档描述 `Assets/Scripts/Network/` 模块的架构。属于模块设计文档 —— 行为规则见 [AGENTS.md](../../AGENTS.md)。

> **注意：** 以下内容为写作时的架构快照，类名与子目录会随重构漂移；以实际代码为准。

## 职责

Network 提供**传输无关**的客户端/服务端网络入口、传输抽象（KCP/TCP）、消息序列化与分发。它是 `SubSystemBase` 子系统，由 `Global` 定位器统一获取。Network 只负责字节流收发与消息分发，**不**包含 gameplay 同步逻辑（同步在 `GamePlay/NetworkSync/`，见 [simulation.md](simulation.md)）。

## 目录结构

```
Network/
├── Client/                  # NetClient
├── Server/                  # NetServer
├── Transport/               # 传输实现
│   ├── Kcp/                 # KcpClientTransport / KcpServerTransport
│   └── Tcp/                 # TcpClientTransport / TcpServerTransport
├── Config/                  # 网络配置
├── Interface/               # IClientTransport / IServerTransport
├── Protocol/                # .proto 定义（net_connect.proto 等）
└── MessageProcessor.cs      # 消息分发
```

## 核心抽象

### 客户端/服务端入口

- **`NetClient`**（`Client/`）：客户端网络入口，继承 `SubSystemBase`，优先级为 `NetWorkManager`。
- **`NetServer`**（`Server/`）：服务端网络入口，同样继承 `SubSystemBase`，优先级为 `NetWorkManager`。
- 两者经 `Global.Get<NetClient>()` / `Global.Get<NetServer>()` 获取，生命周期由 `SubSystemBase` 的 `_Init()` / `_Destroy()` 驱动（见 [AGENTS.md](../../AGENTS.md)「生命周期」）。

### 传输抽象

- **`IClientTransport`**（`Interface/`）：客户端传输接口。定义 `Type`、`IsRunning`、连接/断线/数据接收回调，以及 `StartClient` / `Send` / `Update` / `Stop`。
- **`IServerTransport`**（`Interface/`）：服务端传输接口。定义服务端连接管理、数据接收、广播、发送与更新。
- **传输实现**（`Transport/`）：
  - `KcpClientTransport` / `KcpServerTransport` —— `TransportType.KCP`。
  - `TcpClientTransport` / `TcpServerTransport` —— `TransportType.TCP`。

### 消息序列化与分发

- 消息基于 **Google.Protobuf**。`.proto` 定义在 `Protocol/`（如 `net_connect.proto`，`syntax = "proto3"`，含可靠连接请求/响应、快速连接请求等）。
- 生成消息位于 `GamePlay/Protocol/Generated/`（勿手改，见 [AGENTS.md](../../AGENTS.md)「禁止」）。
- **`MessageProcessor`**（顶层）：消息分发层。按 `NetEvent` 分发客户端消息，并注册 Protobuf `IMessage` 处理器。
- 收发流程：
  - 发送：`NetUtils.Proto2Bytes(evt, message)` 序列化 → transport 发送（fast/reliable）。
  - 接收：`NetUtils.Bytes2Proto(data)` 反序列化 → `MessageProcessor` 分发。
  - 服务端对 `RELIABLE_CONNECT_REQUEST` 等做特殊处理。

## 依赖方向

```
Framework（Global / SubSystemBase / NetUtils）
        ↑
Network（传输 + 消息分发）
        ↑
GamePlay/NetworkSync（消费网络消息驱动同步）
```

- Network 依赖 Framework（`SubSystemBase`、`NetUtils`）与 Protobuf。
- Network **不依赖** GamePlay/NetworkSync；上层（NetworkSync）订阅 Network 的消息事件来驱动同步。

## 程序集

Network 当前编译进默认 `Assembly-CSharp`。新增 `.asmdef` 须文档化边界与方向 —— 见 [AGENTS.md](../../AGENTS.md)「依赖方向」。
