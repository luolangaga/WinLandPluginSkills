# 插件模板（复制后改这几处）

这是一个能直接跑起来的 WinIsland 插件项目模板：小岛显示图标 + 名称，展开后显示状态和描述（**逐帧**变形动画），点击岛体弹出「超级展开」聚光卡，带设置页和每秒刷新的定时器。

复制到目标文件夹后，按下表改名：

| 位置 | 改什么 |
|------|--------|
| 文件名 / 类名 | `MyPlugin` → 你的插件名（如 `WeatherMini`），`MyPlugin.cs`、`MyPluginView.cs`、`MyPluginSpotlightView.cs` 同步改 |
| `MyPlugin*.cs` | 命名空间 `MyPlugin` → 你的插件名 |
| `plugin.json` | `id`（全小写+短横线）、`name`（中文名）、`entry_dll`（改成 `<类名>.dll`）、`author`、`description`、`icon_glyph`、`homepage`、`tags`；`min_host_version` 保持 `2.1.0`（聚光卡需要）；**如果用了「文件投放」就提到 `2.2.0`** |
| `MyPlugin.csproj` | `RootNamespace`；`PluginTargetDir` 结尾的 `my-plugin` → 插件 id；`WinIslandPluginsDir` → 你机器上宿主的 plugins 目录 |

**项目放哪都行**（推荐 `文档\<插件名>\`）：插件 SDK 从 NuGet 装（`luolan.winland.Core`，主次版本 = 宿主 API 版本，当前 2.2.1），不依赖 WinIsland 源码。唯一**必须改**的是 csproj 里的 `WinIslandPluginsDir` → 指向你机器上宿主的 `plugins` 目录（安装版默认 `%LocalAppData%\Programs\WinIsland\plugins`），这样 `dotnet build` 会直接把插件装进你平时用的 WinIsland。

## 目录里的文件

- `MyPlugin.cs` —— 插件入口（`IslandPluginBase`）：注册常驻内容、设置页（含"动作之前先提交设置值"的示范）、设置监听、定时器、点岛开聚光卡；末尾有一段**注释掉的「文件投放」示例**（想让插件接收拖进来的文件/文本/图片时照它写，见 `references/sdk-api.md` §17）
- `MyPluginView.cs` —— 岛上视图（`IMorphView`）：紧凑/展开两种形态 + **逐帧**变形动画（照抄，不要改成 Storyboard）
- `MyPluginSpotlightView.cs` —— 「超级展开」聚光卡内容（点击岛体后居中弹出的大卡片；独立可视树）
- `MyPlugin.csproj` —— 工程文件：从 NuGet 引用插件 SDK（`luolan.winland.Core 2.2.1`）、排除宿主自带的运行时、构建后自动拷进 `WinIslandPluginsDir`
- `plugin.json` —— 插件清单（元数据唯一来源）
- `.gitignore` —— 把 bin/obj 等构建产物排除在 git 之外

## 开发循环

```powershell
dotnet build            # 编译并自动拷贝到宿主 plugins\<id>\
```

然后在 WinIsland：设置 → 插件管理 → 找到插件 → 点「重新加载」。

日志：`%LocalAppData%\WinIsland\logs\plugin.<id>.log`

## 说明：这个 README 的用途

正式投稿社区市场时，**把这个 README 改写成你插件的用户说明**（功能、用法、截图链接、权限说明）。
市场详情页用的就是这个文件，支持标题、列表、表格、代码块、链接；**不支持图片直接渲染**（图片请用绝对链接指向网页）。

写代码的 API 用法见技能里的 `references/sdk-api.md`，出问题看 `references/troubleshooting.md`。
