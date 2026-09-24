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
| 本地构建好好的，装到宿主上却报"版本比宿主新" | `global.json` 是按**当前目录**生效的：在项目目录之外用 `dotnet build <路径>` 调用不受它约束，会挑机器上最新的 SDK（构建日志里能看到 `sdk\11.x...\Sdks\Microsoft.NET.Sdk`） | 先 `cd` 进插件项目目录再 `dotnet build`；在项目目录里 `dotnet --version` 确认是 `10.x` |

## 插件加载阶段

| 现象 | 原因 | 解决 |
|------|------|------|
| 「程序集里没有找到 IIslandPlugin 实现」 | 入口类不是 `public sealed`、是抽象类、缺公共无参构造、或没实现 `IIslandPlugin`/`IslandPluginBase` | 对照 `references/sdk-api.md` 第 3 节改 |
| 「一个包里有多个 IIslandPlugin 实现」 | 引用了别的插件工程或残留了旧类 | 保证一个包只有一个实现类 |
| 「缺少依赖程序集：xxx」 | 第三方依赖没复制到插件目录；或 `deps.json` 缺失；**或构建用的 SDK 比宿主新**（先看这一条：宿主 2.0.x 起会直接报出版本差） | 先在项目目录 `dotnet --version` 确认是 `10.x`（不是就补 `global.json`，见铁律 4）；再打开 `CopyLocalLockFileAssemblies`；确认 `xxx.dll` 和 `<入口>.deps.json` 都被拷进 `plugins\<id>\` |
| 「插件需要 X a.b.c，宿主提供的是 X d.e.f」；或加载报 `FileNotFoundException`、静态构造报 `TypeInitializationException` | 插件构建用的 .NET SDK 比宿主新（正式版宿主由 CI 用 `.NET 10.0.x` 构建），引用的投影程序集（`Microsoft.Windows.SDK.NET` / `WinRT.Runtime`）版本高于宿主，而 .NET 不允许向下绑定强命名程序集 | 项目根目录（`.csproj` 同级）放 `global.json` 钉住 .NET 10（模板自带，别删）→ `dotnet --version` 确认变成 `10.x` → 重新 `dotnet build`。详见 `references/sdk-api.md` §12 |
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
| **大岛变小之后卡死**：岛体收回了、插件元素卡在中间值、不再响应 hover；日志报 `COMException (0x800F1001): Invalid attribute value Unknown for property Height` | 形态动画用了 `Storyboard` 属性路径动画 —— 动态加载的插件程序集解析不出属性所属类型，异常在**动画 tick 里**抛出，调用点的 try/catch 拦不住，会冒到宿主的未处理异常处理器并打断状态机；失败几次后**插件被自动停用** | 改成逐帧直接赋值（`DispatcherQueueTimer` 每帧给 `Height`/`Opacity`/`FontSize` 赋值），见 `references/sdk-api.md` §6；**纯代码建的界面同样不能用 Storyboard**，模板 `MyPluginView.cs` 就是逐帧写法，照抄 |
| 改了设置（城市/关键词…）直接点「立即刷新」，刷的还是旧值 | 设置只在 `LostFocus` 里保存，而按钮点击**先于**失焦发生 | 动作之前显式提交输入框的值（先 `CommitText(textBox)` 再刷新），见 `references/sdk-api.md` §15.1 |
| 改完设置没刷新 / 日志里一条「城市变更」都没有（请求像被吞了） | 刷新用了「忙就 return」的单飞模式（`Interlocked.Exchange(_busy, 1) == 1`），并发时把后来的请求**静默丢弃** | 改成 `SemaphoreSlim(1,1)` 排队串行化，绝不丢弃；每次刷新带 `reason` 写日志，见 §15.2 |
| 设置页的状态还停在上一座城市 / 上一次的数据 | 刷新结果没有回推给还开着的设置页 | 插件暴露快照事件，设置页订阅并在 `Unloaded` 退订，见 §15.1 |
| XAML 视图报找不到 `InitializeComponent` | 动态加载的程序集不在 `resources.pri` 里 | 用 `PluginXaml.Load(this)` 代替 |
| 大岛尺寸/布局被"拉宽拉高" | 宿主会统一展开尺寸（取所有内容最大值） | 视图用自适应布局（`*` 行列 + `Stretch`），不要写死尺寸 |
| 聚光卡（超级展开）点不出来 / 一片空白 | ① `Content` 复用了岛视图那个 `UIElement`（每个窗口一棵树，必须新建视图）② `plugin.json` 的 `min_host_version` 没写 ≥ `2.1.0`，宿主太旧没有这个 API（日志里是 `MissingMethodException`） | 用独立视图 + 提高 `min_host_version`；详见 `references/sdk-api.md` §16 |
| 聚光卡关不掉 | 卡片是模态的：点卡片外区域或按 `Esc` 收起 | 插件也可以自己调 `Context.Island.CloseSpotlight()` |
| 拖东西到岛上完全没反应（岛不展开） | ① 用户在「设置 → 通用 → 文件投放」里关掉了这个功能（总开关）② 拖的是不支持的载荷（HTML 片段、自定义格式）—— 只有文件 / 文本 / 图片会打开投放面板 | 先让用户确认那个开关是开的；日志里能看到「拖入的载荷读不出来」之类的说明 |
| 面板出来了，但**我插件的卡片没出现** | 载荷类型 / 扩展名不匹配：不匹配的卡片是**不显示**（不是变暗） | 检查 `Kinds`（默认只收 `Files`，收文本/图片要显式加）与 `Extensions`（只对文件生效）；拖一段文字测试时文件类卡片本来就不该出现 |
| 卡片出现了，松手却什么都没发生 | ① 松手时没对准卡片（落在摘要区/空白处 = 主动取消）② 拖得太快，载荷还没读完就松手，复核时发现不匹配 | 让用户对准卡片再松手；日志里会写「已忽略：它不接受…」 |
| 卡片里没有我想要的提示文案 | `Handler` 返回 `null` 表示不提示 | 返回字符串（例如 `"已加入 3 首"`）宿主会弹一条临时消息 |
| `Handler` 里改了界面状态却报跨线程异常 | `Handler` 确实在 UI 线程被调用，但如果你在里面 `await` 了带 `ConfigureAwait(false)` 的调用，后续就跑到后台线程了 | 回到 UI 线程再动界面：`Context.RunOnUI(...)`；或别用 `ConfigureAwait(false)` |
| 关掉聚光卡后定时器还在跑 / 还在请求网络 | 宿主收起卡片时会把内容从可视树卸下，但不会替你停表 | 在视图 `Unloaded` 里停定时器、在 `OnClosed` 里取消网络请求 |
| 岛体出现奇怪的底色/色块 | 视图根元素用了不透明背景 | 根元素保持透明，卡片/徽标用半透明白（如 `#33FFFFFF`）适配两种材质 |
| 插件反复出错被自动停用 | 单次会话内未处理异常达到 5 次（**动画 tick / 定时器回调里抛的异常同样计入**） | 看日志找异常源头；初始化必须 10 秒内完成 |
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
