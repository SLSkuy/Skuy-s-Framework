# Network Layer

This document describes `Assets/Scripts/Network/`. It is a module reference; behavior rules are in [AGENTS.md](../../AGENTS.md).

> **Note:** This is an architecture snapshot. Names and subdirectories may drift; verify against the actual code.

## Responsibility

Network provides transport-independent client/server entry points, transport abstractions (KCP/TCP), message serialization, and dispatch. It is exposed as `SubSystemBase` services through `Global`. Network handles byte transport and message dispatch; gameplay synchronization belongs to `GamePlay/NetworkSync/` as character replication (see [simulation.md](simulation.md)).

## Directory Structure

```text
Network/
├── Client/                  # NetClient
├── Server/                  # NetServer
├── Transport/
│   ├── Kcp/
│   └── Tcp/
├── Config/
├── Interface/
├── Protocol/                # .proto definitions
└── MessageProcessor.cs
```

## Core Abstractions

### Client and Server Entry Points

- **`NetClient`** in `Client/` is a `SubSystemBase` client entry point with `NetWorkManager` priority.
- **`NetServer`** in `Server/` is the equivalent server entry point.
- Retrieve both through `Global.Get<NetClient>()` / `Global.Get<NetServer>()`; lifecycle is driven by `SubSystemBase._Init()` / `_Destroy()`.

### Transport Abstractions

- **`IClientTransport`** defines transport type, running state, connection/disconnection/data callbacks, `StartClient`, `Send`, `Update`, and `Stop`.
- **`IServerTransport`** defines server connection management, receive, broadcast, send, and update operations.
- `KcpClientTransport` / `KcpServerTransport` use `TransportType.KCP`; TCP variants use `TransportType.TCP`.

### Serialization and Dispatch

Messages use **Google.Protobuf**. `.proto` definitions live under `Protocol/`; generated messages live under `GamePlay/Protocol/Generated/` and must not be edited manually.

`MessageProcessor` dispatches client messages by `NetEvent` and registers Protobuf `IMessage` handlers. The normal path is:

```text
NetUtils.Proto2Bytes -> transport send
transport receive -> NetUtils.Bytes2Proto -> MessageProcessor
```

The server handles special events such as `RELIABLE_CONNECT_REQUEST`.

## Dependency Direction

```text
Framework (Global / SubSystemBase / NetUtils)
        ↑
Network (transport + message dispatch)
        ↑
GamePlay/NetworkSync (consumes network messages)
```

Network depends on Framework and Protobuf, but not on GamePlay/NetworkSync. NetworkSync consumes Network events from above.

## Assembly

Network currently compiles into the default `Assembly-CSharp`. Document boundaries and direction before adding an `.asmdef`; see [AGENTS.md](../../AGENTS.md).
