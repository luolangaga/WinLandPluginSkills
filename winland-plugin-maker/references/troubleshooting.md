# 常见问题排查

按"现象"查。排错顺序通用建议：先看日志 `%LocalAppData%\WinIsland\logs\plugin.<id>.log`（或插件管理卡片上的「查看日志」），再对照下表。

## 编译/构建阶段

| 现象 | 原因 | 解决 |
|------|------|------|
| `NETSDK1045` / 不认识 `net10.0-windows10.0.26100.0` | 没装 .NET 10 SDK，或版本太老 | 装 .NET 10 SDK；`dotnet --list-sdks` 确认有 `10.x` |
| 找不到 `Microsoft.WindowsAppSDK` / 还原失败 | 网络问题或 NuGet 源不通 | 换网络/代理重试；`dotnet nuget list source` 检查源 |
| 找不到 `WinIsland.Core` | 项目不在 samples 下，相对路径失效 | 改 csproj 里 `WinIslandCoreProject` 为 `WinIsland.Core.csproj` 的真实路径（绝对路径也行） |
| 编译通过但 `plugins\<id>\` 里没有文件 | 拷贝目标路径不对 | 检查 csproj 的 `WinIslandPluginsDir` 指向真实的宿主目录；`dotnet build` 输出里看 CopyToWinIsland 是否执行 |
| `dotnet build` 报文件被占用 | WinIsland 正在运行，dll 锁着 | 关掉 WinIsland 再 build，或先禁用该插件 |

## 插件加载阶段

| 现象 | 原因 | 解决 |
|------|------|------|
| 「程序集里没有找到 IIslandPlugin 实现」 | 入口类不是 `public sealed`、是抽象类、缺公共无参构造、或没实现 `IIslandPlugin`/`IslandPluginBase` | 对照 `references/sdk-api.md` 第 3 节改 |
| 「一个包里有多个 IIslandPlugin 实现」 | 引用了别的插件工程或残留了旧类 | 保证一个包只有一个实现类 |
| 「缺少依赖程序集：xxx」 | 第三方依赖没复制到插件目录；或 `deps.json` 缺失 | 工程打开 `CopyLocalLockFileAssemblies`；确认 `xxx.dll` 和 `<入口>.deps.json` 都被拷进 `plugins\<id>\` |
| 「插件针对 WinIsland.Core x.y 构建，与宿主不兼容」 | 用了 1.x 的 NuGet 包，或旧版编译的 dll | 用源码里的 `WinIsland.Core`（api_version 2）重新编译 |
| 「检测到旧格式 DLL」 | 把 dll 散装在 `plugins\` 根目录了 | v2 只认 `plugins\<id>\ + plugin.json` 或 `.lwp` 包，重新打包 |
| plugin.json 校验失败（红色错误状态） | `id` 格式/`version` 格式/`entry_dll` 不对 | 按提示里的字段说明改；`entry_dll` 只能是文件名、不能带路径、不能是 `WinIsland.Core.dll` |
| 插件列表里根本不出现 | 目录位置不对（不在宿主的 `plugins\` 下）、或缺少 `plugin.json` | 用「插件管理 → 打开目录」核对；重启或重开设置窗口刷新列表 |

## 运行/界面阶段

| 现象 | 原因 | 解决 |
|------|------|------|
| 改了代码没生效 | 本地插件目录不热重载 | 插件管理 → 「重新加载」，或禁用→启用，或重启 WinIsland |
| 「已忽略调用 xxx：插件当前状态为 已停用」 | 停用后定时器/网络回调还在跑 | 属于宿主的保护行为；检查自己的定时器/订阅是否在 `ShutdownAsync` 里收干净（用 `Context.CreateTimer` / `Context.Register` 让宿主自动管） |
| 展开动画卡顿/元素闪现 | 元素在动画中才被加进/移出可视树；或用了 `Visibility = Collapsed` | 所有元素一开始就常驻可视树，隐藏用 `Height = 0 + Opacity = 0` |
| 形态动画执行失败 / 日志报 `E_XAMLPARSEFAILED`、`Invalid attribute value Unknown for property Height` | 在**插件 XAML** 的树上用了 Storyboard 属性路径动画 | 改用逐帧属性赋值（参考 `samples/XamlPlugin`），或在代码里构建 UI 用 Storyboard |
| XAML 视图报找不到 `InitializeComponent` | 动态加载的程序集不在 `resources.pri` 里 | 用 `PluginXaml.Load(this)` 代替 |
| 大岛尺寸/布局被"拉宽拉高" | 宿主会统一展开尺寸（取所有内容最大值） | 视图用自适应布局（`*` 行列 + `Stretch`），不要写死尺寸 |
| 岛体出现奇怪的底色/色块 | 视图根元素用了不透明背景 | 根元素保持透明，卡片/徽标用半透明白（如 `#33FFFFFF`）适配两种材质 |
| 插件反复出错被自动停用 | 单次会话内未处理异常达到 5 次 | 看日志找异常源头；初始化必须 10 秒内完成 |
| 帧率之类的数据读不到 / 显示 `--` | 部分系统数据需要管理员权限 | 以管理员身份运行 WinIsland（例如 ETW 读前台窗口帧率） |
| 中文显示成乱码 | 源文件不是 UTF-8 | 把 .cs / .json 都保存为 UTF-8（含 BOM 更稳） |

## 打包/上传阶段

| 现象 | 原因 | 解决 |
|------|------|------|
| .lwp 包 40MB+ | 把 WinAppSDK/WinUI 运行时打进去了 | 用 `tools/pack-plugin.ps1` 或市场的 `submit-plugin.ps1`（自带过滤）；手动打包时排除 `Microsoft.Windows.*`、`Microsoft.WinUI.dll`、`Microsoft.WindowsAppRuntime*` 等 |
| 提示包里含 `WinIsland.Core.dll` | 宿主契约程序集不该进包 | 从包里删掉；csproj 的 `ProjectReference` 保持 `Private="false"` |
| 市场 CI 校验失败 | `version` 格式 / `entry_dll` / 目录名与 `id` 不一致 | 见 `references/publish.md` 4.6 |
| `pwsh` 不是可识别的命令 | 没装 PowerShell 7 | 用 `powershell` 代替，或 `winget install --id Microsoft.PowerShell` |
| `gh: command not found` | 没装 GitHub CLI | `winget install --id GitHub.cli`，装完重开终端 |

## 拿不准时

1. 先看宿主仓库的 `PLUGIN.md`（插件开发指南）和 `samples/` 下的三个官方样例——它们是最权威的写法参考
2. 内置模块 `WinIsland/Modules/`（电池、媒体、消息）是生产级实现，动画、设置页、生命周期处理都可以直接借鉴
3. 把自己写的代码和样例对照：命名空间、csproj 属性、`IslandPluginBase` 重写方法名，逐项核对
