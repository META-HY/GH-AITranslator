# GH-AITranslator

Grasshopper 组件 AI 汉化翻译插件。在画布上为每个组件叠加中文标签，
内置词库零延迟命中，未翻译的组件通过 OpenAI 兼容协议兜底，命中后
永久缓存到本地。

## 功能特性

| 功能 | 说明 |
|---|---|
| **标签叠加显示** | 在组件上方叠加中文标签，零侵入、不改源文件、随时开关 |
| **本地零延迟命中** | ~40 个内置原生组件 + 4 个第三方插件词库（Kangaroo2 / Weaverbird / LunchBox / OpenNest） |
| **AI 兜底** | 未翻译的组件首次出现时调用 AI（通义千问 / DeepSeek / OpenAI / 自定义 OpenAI 兼容端点） |
| **永久缓存** | AI 翻译结果写入 `dictionary.json`，关闭 Rhino 后依然命中，零 API 调用 |
| **设置面板** | API Key / 端点 / 模型 / 标签字号 / 文字颜色 / 背景颜色 / 悬停提示 / 词库导入导出 |
| **一键翻译** | 菜单 `GH-AITranslator → 翻译当前画布` 一键补齐全部未翻译组件 |
| **第三方词库** | 内置 Kangaroo2 / Weaverbird / LunchBox / OpenNest 共 30+ 条常用组件 |
| **多目标** | 一份代码同时编译 Rhino 7（net48）和 Rhino 8（net7.0-windows） |

## 项目结构

```
GH-AITranslator/
├── src/
│   ├── GHAITranslator.Core/          # 纯 .NET, 无 Rhino 依赖
│   │   ├── Models/                   # ComponentInfo, TranslationEntry, ParamInfo
│   │   ├── ComponentKey.cs           # 稳定 key 生成
│   │   ├── TranslationDictionary.cs  # 线程安全词库 + JSON 持久化
│   │   ├── TranslationDispatcher.cs  # dict-first / AI-fallback, 并发上限 3
│   │   ├── TranslationPipeline.cs    # 同 key 合并并发请求,只发一次 AI
│   │   ├── TranslationSource.cs      # builtin / user / ai 常量
│   │   ├── ProviderRegistry.cs       # 通义千问 / DeepSeek / OpenAI / 自定义
│   │   ├── PromptBuilder.cs          # 中英 Prompt
│   │   ├── PluginSettings.cs         # 用户配置 POCO
│   │   ├── SettingsStore.cs          # JSON 持久化
│   │   ├── PluginPaths.cs            # %AppData%\McNeel\Rhinoceros\<ver>\...
│   │   ├── DictionaryIo.cs           # 词库导入导出 (MergeKeep / MergeOverwrite / Replace)
│   │   ├── Log.cs                    # 轻量文件日志
│   │   ├── BuiltinSeed.cs            # ~40 个原生组件翻译
│   │   ├── Packs/ThirdPartyPacks.cs  # Kangaroo2 / Weaverbird / LunchBox / OpenNest
│   │   └── AI/HttpAiClient.cs        # OpenAI 兼容协议客户端
│   └── GHAITranslator/               # Rhino 集成层
│       ├── GHAITranslatorPlugin.cs   # GH_AssemblyInfo (GUID 已固化)
│       ├── Bootstrapper.cs           # 组合根
│       ├── Integration/
│       │   ├── CanvasLabelRenderer.cs       # Paint-overlay 渲染
│       │   ├── GhDocumentObjectAdapter.cs  # IGH_DocumentObject → ComponentInfo
│       │   └── SettingsMenu.cs              # 菜单 + 一键翻译 + 重载词库
│       └── UI/SettingsPanel.cs             # WinForms 设置面板
└── tests/
    └── GHAITranslator.Tests/         # xUnit, 48 个测试, Linux 可跑
```

## 当前状态：**48/48 单元测试通过**

```
Passed!  - Failed:     0, Passed:    48, Skipped:     0, Total:    48
```

| 测试模块 | 数量 |
|---|---|
| ComponentKey | 8 |
| TranslationDictionary | 6 |
| DictionaryIo (导入导出) | 5 |
| ThirdPartyPacks | 4 |
| HttpAiClient | 6 |
| ProviderRegistry | 4 |
| PromptBuilder | 4 |
| PluginPaths | 2 |
| TranslationDispatcher (含 LocalHit_NeverInvokesAi) | 9 |

Plugin 层 (R7/R8 兼容代码) **dotnet build 在 Linux 容器里也已经通过**——
只是没办法跑出真 `.gha`,因为 `dotnet build` 的输出是 `.dll`,Grasshopper
要求扩展名是 `.gha`,且最终产物在 Windows Rhino 进程里加载。

## 在 Windows 上构建 .gha

### 方法 A：本地手动构建

```powershell
# 1. 装 .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# 2. 装 Rhino 7 或 Rhino 8 (试用 90 天也行)
#    https://www.rhino3d.com/download/rhino-for-windows/

# 3. 拉代码,构建
git clone <repo-url> GH-AITranslator
cd GH-AITranslator
dotnet build src/GHAITranslator/GHAITranslator.csproj -c Release -f net7.0-windows -p:EnableWindowsTargeting=true

# 4. 拷贝 .dll 为 .gha (Grasshopper 凭扩展名识别)
copy src\GHAITranslator\bin\Release\net7.0-windows\GH-AITranslator.dll `
     %APPDATA%\Grasshopper\Libraries\GH-AITranslator.gha

# 5. 重启 Rhino 8 → Grasshopper 会自动加载
```

### 方法 B：GitHub Actions 自动构建（推荐）

`.github/workflows/build-gha.yml` 已配置：

1. Push 到 main / 推 `v*` tag → CI 自动跑
2. 在 Actions 页面下载 `gha-build` artifact，包含：
   - `GH-AITranslator-rhino7.gha`（Rhino 7, .NET Framework 4.8）
   - `GH-AITranslator-rhino8.gha`（Rhino 8, .NET 7）
3. 推 `v1.0.0` tag 时自动挂到 GitHub Release

## 安装与使用

### 安装

```powershell
# 把 .gha 扔进 Grasshopper Libraries 目录
copy GH-AITranslator-rhino8.gha %APPDATA%\Grasshopper\Libraries\

# 重启 Rhino → Grasshopper 顶部菜单应出现 "GH-AITranslator"
```

### 首次配置

1. 菜单 `GH-AITranslator → 设置...`
2. 在「API 密钥」里填入通义千问 / DeepSeek / OpenAI 任一家的 Key
3. 服务商切换会自动填充默认端点和模型，确认即可
4. 点击「保存」

### 使用流程

- **打开 / 打开 .gh 文件** → 画布上的内置组件（Point / Line / Surface 等）立刻出现中文标签
- **菜单 `GH-AITranslator → 翻译当前画布`** → 一键把画布上所有未翻译的组件交给 AI，弹出报告「共 N 个 / 本地命中 X / AI 翻译 Y / 失败 Z」
- **菜单 `GH-AITranslator → 重载第三方词库`** → 如果你装了 Kangaroo2 / Weaverbird 等插件，重载后再翻译就能命中它们的常用组件

### 数据落盘位置

```
%APPDATA%\McNeel\Rhinoceros\8.0\Plug-ins\GH-AITranslator\
├── dictionary.json      # 词库（builtin + user + ai 三类混存）
└── settings.json        # 用户配置（API Key 等）
```

## Linux 端开发流程

Linux 上只能跑 Core 库 + 测试,Plugin 层只能 build 不能跑：

```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1   # 容器内无 libicu
dotnet test tests/GHAITranslator.Tests/GHAITranslator.Tests.csproj

# Plugin 层编译验证（输出是 .dll,不是 .gha,只是检验兼容性）
dotnet build src/GHAITranslator/GHAITranslator.csproj -c Debug -p:EnableWindowsTargeting=true
```

## 已知约束

| 约束 | 影响 |
|---|---|
| **必须 Windows + Rhino** | Grasshopper SDK 仅 Windows，最终加载测试需要 Rhino 7/8 桌面 |
| **AI 需要 Key** | 不填 Key 也能用本地词库，只是第三方 / 未翻译组件不会自动翻译 |
| **标签叠加 ≠ 改源文件** | 翻译结果只影响画布显示，不会修改组件的 NickName / Description，避免污染源 .gh 文件 |

## Roadmap

- [ ] P1：Hover 显示中文描述
- [ ] P1：组件搜索面板（中文搜索原生组件库）
- [ ] P2：属性替换模式（实验性，反射改 NickName — 会污染源文件）
- [ ] P2：更多第三方插件词库（Ladybug / Honeybee / Karamba3D / Clipper）
- [ ] P3：GitHub Release 一键安装脚本