# 插件模板（复制后改这几处）

这是一个能直接跑起来的 WinIsland 插件项目模板：小岛显示图标 + 名称，展开后显示状态和描述，带设置页和每秒刷新的定时器。

复制到目标文件夹后，按下表改名：

| 位置 | 改什么 |
|------|--------|
| 文件名 / 类名 | `MyPlugin` → 你的插件名（如 `WeatherMini`），`MyPlugin.cs`、`MyPluginView.cs` 同步改 |
| `MyPlugin.cs` / `MyPluginView.cs` | 命名空间 `MyPlugin` → 你的插件名 |
| `plugin.json` | `id`（全小写+短横线）、`name`（中文名）、`entry_dll`（改成 `<类名>.dll`）、`author`、`description`、`icon_glyph`、`homepage`、`tags` |
| `MyPlugin.csproj` | `RootNamespace`；`PluginTargetDir` 结尾的 `my-plugin` → 插件 id；`WinIslandPluginsDir` → 你机器上宿主的 plugins 目录 |

**项目放在 WinIsland 源码仓库的 `samples\<名字>\` 下时，`WinIslandCoreProject` 和 `WinIslandPluginsDir` 的默认相对路径开箱即用；放到别处就要把这两个路径改对。**

## 目录里的文件

- `MyPlugin.cs` —— 插件入口（`IslandPluginBase`）：注册常驻内容、设置页、设置监听、定时器
- `MyPluginView.cs` —— 岛上视图（`IMorphView`）：紧凑/展开两种形态 + 变形动画
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
