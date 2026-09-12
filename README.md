<div align="center">

# **游戏开发框架**

[![Unity](https://img.shields.io/badge/Unity-6000.5.6f1-57b9d3.svg?logo=unity&style=flat-square)](https://unity.com/)
[![URP](https://img.shields.io/badge/URP-17.5.0-2296F3.svg?style=flat-square)](https://unity.com/srp/universal-render-pipeline)
[![ECS](https://img.shields.io/badge/Entities-1.0.0-FF6F61.svg?style=flat-square)](https://unity.com/products/unity-entities)
[![HybridCLR](https://img.shields.io/badge/HybridCLR-latest-FF4081.svg?style=flat-square)](https://hybridclr.doc.code-philosophy.com/)

</div>

---

## 项目简介

一套个人Unity开发框架，苦逼迭代中

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

3. **验证编译**
   - 打开后查看 Console，确认无编译错误
   - 菜单栏 `Edit` → `Project Settings` → `Player` → 检查 `Scripting Backend` 为 IL2CPP（HybridCLR 依赖）