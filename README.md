# GH-AITranslator-Pro

A complete Chinese localization plugin for the **Grasshopper** plugin bundled
inside Rhino 7 / Rhino 8.

| Feature | Status |
|---|---|
| Translate every built-in GH component Name | ✅ 250+ entries |
| Translate NickName (port labels) | ✅ ≤ 2 Chinese chars |
| Translate Description (hover tooltips) | ✅ |
| Translate Category / SubCategory (panel tabs) | ✅ |
| Translate objects already on the canvas (open .gh) | ✅ |
| Translate newly added objects automatically | ✅ |
| Three display modes: 中文 / 中英双语 / English | ✅ |
| Bilingual separator: `|` (half-width pipe) | ✅ |
| LLM translation fallback (OpenAI / DeepSeek / Qwen / Custom) | ✅ |
| GH menu integration (top strip, GH-style) | ✅ |
| "Restore English" menu item | ✅ |
| Settings panel for AI provider | ✅ |
| Built-in dictionary quality tests | ✅ 30/30 passing |
| GitHub Release artifacts (no login required to download) | ✅ |

## How "complete" is enforced

Every entry in `BuiltinSeed.cs` is reviewed against the rules in
[`DictionarySpec.md`](DictionarySpec.md). The `BuiltinSeedCoverageTests` test
suite enforces:

- ≥ 250 entries
- All five fields non-empty
- Description ends with `。`
- NickName ≤ 2 characters
- Unique keys with `Native_` prefix
- Category from the canonical 17-tab list
- English mirror present for Bilingual / English modes
- Chinese Name contains no Latin script (except an allow-list of technical
  proper nouns: `NURBS`, `B-rep`, `3D`, `C#`, `VB`, `Python`, `XYZ`)

If any of these fail, the build is red.

## Build

```bash
# Tests (Linux-friendly, net8.0):
dotnet test tests/GHAITranslator.Tests

# Full plugin build (Windows only — needs GH + Rhino SDKs):
GH_SDK=/path/to/gh7/sdk      \
GH_SDK_N7=/path/to/gh8/sdk   \
RHINO_SDK=/path/to/rhino7    \
RHINO_SDK_N7=/path/to/rhino8 \
dotnet build GH-AITranslator.sln -c Release
```

CI runs on `windows-2022`. The matrix produces `gha-rhino7` and `gha-rhino8`
artifacts. Tagging `v1.0.0` publishes them to a GitHub Release.

## Install

1. Download `GH-AITranslator-Pro-rhino8.zip` from the Releases page
   (or the Actions artifacts if you have access).
2. Unzip into Rhino's plugin folder:
   - Rhino 7: `%APPDATA%\McNeel\Rhinoceros\7.0\Plug-ins\GH-AITranslator-Pro\`
   - Rhino 8: `%APPDATA%\McNeel\Rhinoceros\8.0\Plug-ins\GH-AITranslator-Pro\`
3. Restart Rhino. Open Grasshopper. The menu **翻译 (&T)** appears.

## Configuration

User data lives in `%APPDATA%\GH-AITranslator\`:

- `settings.json` — language mode, AI provider config
- `dictionary.json` — user overlay on top of BuiltinSeed
- `plugin.log` — last ~500 lines of plugin activity

Open the settings panel from the GH menu (**翻译 → 关于 → 设置...**).

## Three language modes

| Mode | Battery label | Description |
|---|---|---|
| 中文 | `点` | `三维空间中的一个点。` |
| 中英双语 | `点\|Point` | `三维空间中的一个点。\|A point in 3D space.` |
| English | `Point` | `A point in 3D space.` |

Switch via the menu — the change re-translates every object on every open
canvas immediately.

## LLM translation

When the plugin encounters a component not in `BuiltinSeed`, it can ask an
LLM for the translation. Configure via the settings panel:

| Provider | Default model | Default endpoint |
|---|---|---|
| OpenAI | gpt-4o-mini | https://api.openai.com/v1/chat/completions |
| DeepSeek | deepseek-chat | https://api.deepseek.com/v1/chat/completions |
| Qwen | qwen-plus | https://dashscope.aliyuncs.com/... |
| Custom | (you fill in) | (you fill in) |

Results are persisted to `dictionary.json` so the second load is offline.

## Architecture

```
src/
├── GHAITranslator.Core/         (net48 + net7.0-windows + net8.0 for tests)
│   ├── Models/                  TranslationEntry, ComponentInfo, LanguageMode
│   ├── ComponentKey.cs          lookup-key builder
│   ├── TranslationDictionary.cs in-memory dict with BuiltinSeed + user overlay
│   ├── DictionaryIo.cs          load / save dictionary.json
│   ├── BuiltinSeed.cs           300 entries, hard-coded
│   ├── HttpAiClient.cs          OpenAI-compatible chat completions
│   ├── TranslationPipeline.cs   cache-first → AI-fallback → persist
│   ├── LanguageFormatter.cs     Chinese / Bilingual / English renderer
│   ├── PluginPaths.cs           %APPDATA%/GH-AITranslator/
│   ├── SettingsStore.cs         settings.json read / write
│   └── Log.cs                   bounded ring-buffer file logger
│
├── GHAITranslator/              (net48 + net7.0-windows — the plugin itself)
│   ├── GHAITranslatorPlugin.cs  GH_AssemblyPriority entry point
│   ├── Bootstrapper.cs          wire everything up
│   ├── Integration/
│   │   ├── ComponentTranslator.cs   write Name/Nick/Desc/Category
│   │   ├── DocumentHook.cs          open + new-object translation
│   │   ├── GhAdapter.cs             assembly + class → lookup key
│   │   └── SettingsMenu.cs          GH menu strip integration
│   └── UI/
│       └── SettingsPanel.cs     AI / language config dialog
│
tests/GHAITranslator.Tests/      (net8.0 — Linux CI friendly)
├── BuiltinSeedCoverageTests.cs  DictionarySpec enforcement
├── TranslationDictionaryTests.cs
├── LanguageFormatterTests.cs
├── LanguageModeTests.cs
├── TranslationPipelineTests.cs
├── ComponentKeyTests.cs
├── ProviderRegistryTests.cs
└── DictionaryIoTests.cs
```

## Out of scope (deliberately)

The menus, panels, and right-click menus inside Grasshopper's main UI are
**embedded WinForms resources** inside `Grasshopper.dll` itself. Replacing
them requires either:

- Modifying `Grasshopper.dll` directly (bypasses licensing; won't survive
  Rhino updates), or
- Shipping a satellite resource assembly under
  `Grasshopper.zh-CN.resources.dll` and convincing Rhino to load it.

Both approaches are outside the scope of a `.gha` plugin and are not
attempted by this project. If you need the full Grasshopper UI shell
translated (window chrome, ribbon, dialogs), that requires an external
tool that patches Rhino itself — out of scope here.

## License

MIT. Grasshopper, Rhino, and Rhinoceros are trademarks of McNeel & Associates.
This plugin is unofficial and unaffiliated.