<div align="center">

# Skuy's Framework

**游戏开发框架**

[![Unity](https://img.shields.io/badge/Unity-6000.5.6f1-57b9d3.svg?logo=unity&style=flat-square)](https://unity.com/)
[![URP](https://img.shields.io/badge/URP-17.5.0-2296F3.svg?style=flat-square)](https://unity.com/srp/universal-render-pipeline)
[![ECS](https://img.shields.io/badge/Entities-1.0.0-FF6F61.svg?style=flat-square)](https://unity.com/products/unity-entities)
[![HybridCLR](https://img.shields.io/badge/HybridCLR-latest-FF4081.svg?style=flat-square)](https://hybridclr.doc.code-philosophy.com/)

</div>

---

## 项目简介

Skuy's Framework 是一套围绕 **多人游戏** 构建的 Unity 原生开发框架。核心围绕「服务端权威 + 客户端预测 + 远端插值」的网络同步架构展开，同时内置 ECS 级 A\* 寻路、UI 分层框架、资源对象池、HybridCLR 热更新预留等生产级能力。

---

## 目录结构

```
Skuy's Framework/
├── Assets/
│   ├── Animations/              # Animator Controller + FBX 动画（Rifle/Sword 两套战斗动作）
│   ├── Art/                     # 美术资源（Meshes / Materials / Textures）
│   ├── Configs/                 # 全局配置 ScriptableObject（ResourceConfig / UIConfig）
│   ├── InputActions/            # 新 Input System .inputactions 资产
│   ├── Plugins/Protobuf/        # Google.Protobuf.dll
│   ├── Prefabs/                 # 可复用 Prefab（Camera / Navigation Agent）
│   ├── Resources/               # 运行时 Resources 加载：Config Asset + NetPlayer Prefab + UI Prefab
│   ├── Scenes/
│   │   ├── LaunchScene.unity    # 启动场景（HybridCLR 热更入口）
│   │   ├── GameScene.unity      # 主游戏场景
│   │   └── Dev/                 # 开发调试场景
│   │       ├── Network/SyncTest.unity
│   │       ├── Network/TransportTest.unity
│   │       └── Navigation/Navigation.unity
│   └── Scripts/                 # ✅ 全部运行时代码（单 Assembly-CSharp 程序集）
│       ├── Framework/           # 核心框架层
│       │   ├── Global.cs        # 服务定位器入口
│       │   ├── Common/          # Singleton / StateMachine / SubSystemManager
│       │   ├── SubSystems/      # 9 大子系统（Camera/Resource/UI/...）
│       │   ├── Input/           # InputProvider 体系 + InputState struct
│       │   ├── Navigation/      # AStarECS（Authoring/Components/Systems）
│       │   └── Event/           # EventBus<T>
│       ├── GamePlay/            # 游戏业务层
│       │   ├── EntitySystem/    # 实体系统（架构核心）
│       │   │   ├── Core/        # IEntityIntentReceiver / IEntitySimulation / IEntityStateView
│       │   │   ├── Entities/    # BaseEntity / EntityCharacter / Types + FSM + Config
│       │   │   ├── Controller/  # Player / Authority / Replica / AI
│       │   │   ├── Modules/     # Movement / Animation（IEntityModule）
│       │   │   └── Sync/        # NetEntitySyncRoot / SnapshotBuffer / Interfaces
│       │   ├── MultiPlaySystem/ # 多人组件 / Snapshot / Config
│       │   └── Protocol/        # Protobuf 生成代码（Generated/，勿手改）
│       ├── Network/             # 网络层
│       │   ├── Client/          # NetClient
│       │   ├── Server/          # NetServer + ClientManager
│       │   ├── Transport/       # Kcp/ + Tcp/ + TransportType
│       │   ├── Config/          # NetClientConfig / NetServerConfig / KcpTransportConfig
│       │   ├── Interface/       # IClientTransport / IServerTransport
│       │   ├── Protocol/        # .proto 源文件
│       │   └── MessageProcessor.cs
│       ├── Events/              # 跨模块枚举（NetEvent）
│       ├── Utils/               # 静态工具 + DataStruct/KDTree
│       ├── Core/                # GameCore / GameConstants / GameSettings / GameModel
│       ├── Debug/               # ConsoleToScreen 调试面板
│       ├── Editor/              # 编辑器工具（仅 Editor 编译）
│       │   ├── Protobuf/ProtoCompilerWindow.cs   # .proto → .cs 一键编译
│       │   └── UIFramework/UICodeGenerator.cs    # UI 代码自动生成
│       ├── Tests/               # 运行时调试面板 & 单元测试
│       ├── Launch.cs            # 场景入口（全局命名空间）
│       └── MainEntry.cs         # 主入口（全局命名空间）
├── Docs/                        # ✅ 设计文档
│   └── EntityControl/           # 实体控制系统设计（00-Index.md + 3 篇分章）
├── Packages/                    # UPM manifest.json（含 HybridCLR / unity-mcp）
├── ProjectSettings/             # Unity 项目设置
└── AGENTS.md                    # 📐 本仓库编码规范与架构约定（必读）
```
---

## 安装步骤

1. **克隆仓库**
   ```bash
   git clone <repo-url>
   cd "Skuy's Framework"
   ```

2. **用 Unity Hub 打开项目**
   - Unity Hub → 打开 → 选择 `Skuy's Framework` 根目录
   - 等待 Unity 自动下载 URP / ECS / Cinemachine 等包（首次约需 5~15 分钟）
   - 若 HybridCLR / unity-mcp 拉取失败，检查 `Packages/manifest.json` 中 git URL 的网络访问

3. **验证编译**
   - 打开后查看 Console，确认无编译错误
   - 菜单栏 `Edit` → `Project Settings` → `Player` → 检查 `Scripting Backend` 为 IL2CPP（HybridCLR 依赖）

---

## 设计文档

架构设计文档存放于 **[Docs/](./Docs/)** 目录，采用「一功能一文件夹 + 编号前缀 + 中文内容」组织：

```
Docs/
└── EntityControl/
    ├── 00-Index.md                          # 阅读索引
    ├── 01-Level-1-Net-Sync-Change.md        # 网络同步改造方案
    ├── 02-Level-1-Priority-Issues.md        # 优先级问题清单
    └── 03-Identity-SyncRoot-Controller-Convergence.md  # 身份/根组件/控制器合并
```

> 新增设计文档时，请在对应功能文件夹的 `00-Index.md` 中登记

---

## 使用的第三方库

KCP C#版: https://github.com/KumoKyaku/kcp

UnityURPToonLitShader: https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample