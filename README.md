# Antigravity No-TUN Launcher 🚀

[![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)](https://microsoft.com/windows)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()

> **专为 Antigravity 打造的免 TUN（No-TUN）代理守护与自动修复启动器。**
> 解决每次官方更新覆盖并删除 `version.dll` 与 `config.json` 导致代理失效的问题，支持全自动智能检索本机安装路径，静默无感启动。

---

## 💡 背景与痛点

Antigravity 采用 Electron + 独立后端语言服务器（Language Server / gRPC / Node.js）的架构。在 Windows 下，语言服务器等后台服务常因无法读取系统 PAC / HTTP 代理而导致登录超时或 AI 功能失联。

通过在 Antigravity 目录下注入 `version.dll`（DLL 劫持代理）和 `config.json`（SOCKS5 转发规则），可以实现**无需开启系统级 TUN 虚拟网卡**的精准进程级代理。

**然而存在一个致命痛点**：
Antigravity 每次版本在线更新时，安装器会重新部署主目录并清空非官方文件，导致用户每次更新后都需要手动将 `version.dll` 和 `config.json` 重新复制回安装目录。

本项目通过**智能路径探测 + 启动前自动修补 + 静默拉起**，让整个流程实现 100% 自动化。

---

## ✨ 核心特性

- 🔍 **多策略智能路径自动探测**：
  - 依次检索：**运行中进程** ➔ **Windows 注册表（Uninstall 列表）** ➔ **常见默认安装路径** ➔ **桌面与开始菜单快捷方式** ➔ **GUI 交互选择**。无需用户手动查找或硬编码路径。
- 🛡️ **更新自愈（Auto-Fix）**：
  - 启动前自动检查目标目录是否存在 `version.dll` 和 `config.json`，缺失时毫秒级自动从备份源补齐。
- 🔄 **双向配置同步**：
  - 若用户在 Antigravity 目录下调整了代理端口或路由规则（`config.json`），启动器检测到新修改后会自动同步回备份源，防止更新后自定义配置丢失。
- ⚡ **无黑框静默启动**：
  - 编译为原生 Windows GUI 子系统应用（体积仅约 16 KB），启动时**无控制台黑框闪烁**，且完全透传命令行参数。
- 📦 **多重使用形态**：
  - 提供单文件编译版（`.exe`）、纯 PowerShell 版（`.ps1`）与纯批处理版（`.bat`），满足不同用户的审查与使用需求。
- 🤖 **内置 GitHub Actions**：
  - 推送 Tag 自动打包并发布 GitHub Releases。

---

## 📂 项目结构

```text
antigravity-no-tun-launcher/
├── .github/
│   └── workflows/
│       └── release.yml          # GitHub Actions 自动化编译打包工作流
├── assets/
│   ├── app.ico                  # 应用图标
│   ├── config.json              # 默认代理配置模板（SOCKS5 127.0.0.1:7897 / Fake IP）
│   └── version.dll              # 免 TUN 代理劫持 DLL
├── scripts/
│   ├── launch.bat               # 纯批处理启动器（带路径自动探测）
│   ├── launch.ps1               # PowerShell 启动器（功能完整）
│   └── install_shortcut.bat     # 一键安装桌面快捷方式脚本
├── src/
│   ├── AntigravityLauncher.csproj # 现代 .NET 工程文件
│   ├── PathDetector.cs          # 核心：五级路径智能探测器
│   ├── Patcher.cs               # 核心：文件校验、自动恢复与双向同步
│   ├── ShortcutManager.cs       # 桌面与开始菜单快捷方式管理
│   └── Program.cs               # 程序入口与参数解析
├── build.ps1                    # 本地一键编译与打包脚本
├── .gitignore                   # Git 忽略配置
├── LICENSE                      # MIT 开源许可证
└── README.md                    # 项目说明文档
```

---

## 🚀 快速上手

### 方式一：真·单文件绿色版（最推荐，免解压）

1. 前往 GitHub [Releases](../../releases) 页面**仅需直接下载单个 `AntigravityLauncher.exe` 文件**。
2. 将其放置在任意你喜欢的位置（如 `D:\Tools\` 或程序目录）。
3. **直接双击运行**即可自动定位 Antigravity 并在缺失时自动补齐配置文件！
4. **生成桌面快捷方式**：只需在终端或运行窗口执行一次 `AntigravityLauncher.exe --install-shortcut`，即可在桌面与开始菜单自动生成官方图标的专用快捷方式。

### 方式二：下载完整压缩包（适合需要预设自定义配置的用户）

1. 前往 GitHub [Releases](../../releases) 下载 `Antigravity-No-TUN-Launcher.zip` 并解压。
2. 如需修改默认代理端口：打开 `assets/config.json`，将 `proxy.port` 修改为你本地客户端的实际端口（默认为 `7897`，如 Clash Verge / Mihomo 常为 7897，v2rayN 常为 10808）。
3. 双击运行 `install_shortcut.bat` 即可一键生成桌面专用快捷方式。

### 方式三：命令行参数

启动器支持以下命令行参数：

| 参数 | 说明 |
| :--- | :--- |
| *(无参数)* | 自动探测路径、检查补齐文件并直接启动 Antigravity |
| `--detect` | 仅输出并显示检测到的 Antigravity 安装目录，不执行启动 |
| `--install-shortcut` | 在桌面及开始菜单创建 `Antigravity (No-TUN)` 快捷方式 |
| `--path "<路径>"` | 手动强制指定 Antigravity 所在目录 |
| `--help` / `-h` | 查看帮助提示 |

> 提示：其他任何未识别的参数均会自动透明透传给 `Antigravity.exe`（例如工程路径参数 `AntigravityLauncher.exe "D:\my-project"`）。

---

## 🛠️ 本地编译与构建

本项目支持多种编译方式：

### 1. Windows 原生编译（无需安装 Visual Studio 或额外 SDK）

项目提供了通过 Windows 系统自带的 `csc.exe`（.NET Framework）一键生成独立 exe 的功能：

```powershell
.\build.ps1 -Mode csc
```
产物将输出至 `dist/AntigravityLauncher.exe`，文件仅 16 KB 左右。

### 2. 现代 .NET 编译（.NET 8.0 / 10.0）

```powershell
.\build.ps1 -Mode dotnet
```

---

## ❓ 常见问题 (FAQ)

### Q1: 运行后提示未找到 Antigravity 安装目录？
通常情况下启动器会通过注册表和快捷方式全自动识别。如果你的 Antigravity 是绿色便携版或解压到特殊目录，启动器会自动弹出文件选择框，手动选择一次 `Antigravity.exe` 即可，后续启动会自动记忆。

### Q2: 报毒 / 误报说明
`version.dll` 是通过 Windows 标准的 DLL 劫持/代理机制工作的，部分敏感的杀毒软件可能会提示风险。你可以自行检查源代码或为该目录添加信任白名单。

---

## 📄 开源许可证

本项目基于 [MIT License](LICENSE) 开源。
