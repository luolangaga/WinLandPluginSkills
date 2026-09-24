# WinIsland 插件 SDK 2.0 参考（插件作者版）

写代码前通读本文。所有类型都在 `WinIsland.Core` 命名空间下。**2.0 与 1.x 不兼容**，文末有对照表，看到旧写法一律作废。

`api_version` 目前是 `2`；之后新增的能力都是**增量 API**，用 `plugin.json` 的 `min_host_version` 做门槛 —— 用到哪个就把门槛提到对应版本：**聚光卡（§16）要 `2.1.0`**、**文件投放（§17）要 `2.2.0`**。

## 1. 工程怎么引用 SDK

**从 NuGet 装**（推荐，插件项目**不需要 WinIsland 源码**）：

```xml
<ItemGroup>
  <PackageReference Include="luolan.winland.Core" Version="2.2.1" PrivateAssets="all" ExcludeAssets="runtime" />
</ItemGroup>
```

或者在插件项目目录里跑 `dotnet add package luolan.winland.Core`（不写 `--version` 就装最新版）。

- **版本号怎么对**：SDK 包的**主次版本号 = 宿主 API 版本** —— `2.2.x` = 基础能力 + 聚光卡（§16）+ 文件投放（§17），补丁位（如 `2.2.0` → `2.2.1`）只是 SDK 包本身的修正，不引入新 API。当前最新 `2.2.1`。
- 用到 2.1 / 2.2 的增量 API 时，`plugin.json` 的 `min_host_version` 要跟着提到 `"2.1.0"` / `"2.2.0"`（**注意：这里写的是宿主版本，跟 SDK 包的补丁号无关**）。
- `ExcludeAssets="runtime"` **是必须的**：`WinIsland.Core.dll` 由宿主提供，不能进插件包（市场 CI 会拒绝）。
- 插件 TFM 必须是 `net10.0-windows10.0.26100.0`（包只提供这个目标框架）。
- **没有源码也能做插件**。只有「想跟着宿主源码一起改 SDK」或「需要源码构建的宿主调试副本」时才需要源码仓库，那时把上面那行换成 `ProjectReference`：

```xml
<ItemGroup>
  <ProjectReference Include="..\..\WinIsland.Core\WinIsland.Core.csproj" Private="false" />
</ItemGroup>
```

csproj 关键属性（和官方样例保持一致）：

```xml
<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
<UseWinUI>true</UseWinUI>
<WinUISDKReferences>false</WinUISDKReferences>
<WindowsPackageType>None</WindowsPackageType>
<EnableMsixTooling>false</EnableMsixTooling>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>

<!-- 自带第三方 NuGet 依赖时必开，否则依赖不会复制到输出目录 -->
<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
```

WinAppSDK 与 SDK.BuildTools 由宿主提供，引用时把它们排除出运行时（否则包会白胖 40MB+）：

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="2.3.1" PrivateAssets="all" ExcludeAssets="runtime" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.28000.2526" PrivateAssets="all" ExcludeAssets="runtime" />
```

## 2. 插件目录结构与 plugin.json

```
plugins/
  hw-monitor/                  ← 一个插件一个目录，目录名 = 插件 id
    plugin.json                ← 清单（唯一元数据来源）
    HardwareMonitor.dll        ← 入口程序集（plugin.json 的 entry_dll）
    HardwareMonitor.deps.json  ← 依赖清单（构建自动生成）
    <私有依赖>.dll
  demo.lwp                     ← 放在 plugins/ 根目录的包，启动时自动安装并删除
```

```json
{
  "id": "hw-monitor",
  "name": "硬件监控",
  "version": "1.0.0",
  "entry_dll": "HardwareMonitor.dll",
  "api_version": 2,
  "min_host_version": "2.0.0",
  "description": "一句话描述（市场列表最多显示两行）",
  "author": "作者名",
  "icon_glyph": "\uE950",
  "homepage": "https://github.com/you/hw-monitor",
  "license": "MIT",
  "tags": ["hardware", "monitor"]
}
```

| 字段 | 必填 | 规则 |
|------|------|------|
| `id` | ✔ | `^[a-z0-9][a-z0-9-]{1,63}$`（全小写、数字、短横线），全局唯一，投稿市场时必须等于目录名 |
| `name` | ✔ | 显示名称，建议 ≤ 12 个字 |
| `version` | ✔ | 数字点分版本号 `1.2.3`；投稿市场中必须与包内那份**完全一致** |
| `entry_dll` | ✔ | 包内文件名，不能含路径；不能是 `WinIsland.Core.dll` |
| `api_version` | ✔ | 当前为 `2` |
| `min_host_version` | ✖ | 低于该宿主版本时拒绝加载 |
| 其余 | ✖ | 展示用 |

校验失败时插件会以 **错误** 状态出现在「插件管理」里并写明原因；日志在 `%LocalAppData%\WinIsland\logs\plugin.<id>.log`。

## 3. 最小插件

```csharp
using WinIsland.Core;

namespace MyPlugin;

public sealed class MyPlugin : IslandPluginBase
{
    protected override Task OnInitializeAsync()
    {
        Log.Info("插件已启动");
        Context.Island.ShowMessage(new IslandMessage { Title = "你好", Text = "来自我的插件" });
        return Task.CompletedTask;
    }

    protected override Task OnShutdownAsync()
    {
        Log.Info("插件已停用");
        return Task.CompletedTask;
    }
}
```

硬性要求：`public sealed class`、实现 `IIslandPlugin`（或继承 `IslandPluginBase`）、有公共无参构造函数。**一个包只允许一个 `IIslandPlugin` 实现**，多了少了都会拒绝加载。

## 4. IPluginContext（插件与宿主交互的唯一入口）

`IslandPluginBase` 里用 `Context` 访问；它同时提供了 `Manifest` / `Log` / `Settings` / `PluginDirectory` / `HostVersion` 快捷属性。

| 成员 | 说明 |
|------|------|
| `Manifest` | 当前插件清单（`PluginManifest`：Id / Name / Version / Description / Author / IconGlyph…） |
| `PluginDirectory` | 插件自己的目录（读内置资源文件用绝对路径） |
| `HostVersion` / `Dispatcher` | 宿主版本号 / UI 线程调度器 |
| `Log` | `Debug/Info/Warn/Error` 写入插件日志（内存 + 文件） |
| `Settings` | 作用域化设置存储，键自动加 `<id>.` 前缀 |
| `Island.SetContent(content)` | 注册常驻内容（`null` 取消）。owner 由宿主绑定为插件 id |
| `Theme` | 岛体当前的明暗主题（`IIslandTheme`：`IsLight` + `Changed`），配色适配用（见 §18，需宿主 ≥ 2.3.0） |
| `Island.OpenSpotlight(spotlight)` / `Island.CloseSpotlight()` | 打开 / 收起「超级展开」聚光卡（见 §16，需宿主 ≥ 2.1.0） |
| `Island.AddDropTarget(target)` / `Island.RemoveDropTarget(id)` | 注册 / 移除**文件投放目标**：拖文件/文本/图片到岛上时的一排卡片（见 §17，需宿主 ≥ 2.2.0；停用时自动移除） |
| `Island.ShowMessage(msg)` | 弹一条临时消息 |
| `Island.Show(uiElement, size, duration)` | 临时展示任意控件 |
| `Island.AddSettingsPage(desc)` / `RemoveSettingsPage(id)` | 注册 / 移除设置页（停用时自动移除） |
| `Register(IDisposable)` | 登记需要在停用时释放的资源（订阅、原生句柄…） |
| `OnSettingsChanged(key, handler)` | 监听某个设置键（handler 在 UI 线程调用） |
| `CreateTimer(interval, repeat, tick)` | UI 线程定时器（停用时自动停止并解绑） |
| `RunOnUI` / `RunOnUIAsync` | 把工作编组回 UI 线程 |

### IslandPluginBase 的额外便利

- `SetContent(IslandLiveContent?)` / `UpdateContent(content)`：带去重的注册/更新
- `RunOnUI(Action)`：等价于 `Context.RunOnUI`
- 重写 `OnInitializeAsync` / `OnShutdownAsync` 即可，不用手写 `InitializeAsync`

## 5. 常驻内容：IslandLiveContent

```csharp
// 单视图变形（推荐）：一个视图同时承载紧凑/展开两种形态
Context.Island.SetContent(new IslandLiveContent
{
    Priority = 60,
    OwnerLabel = Manifest.Name,
    OwnerGlyph = Manifest.IconGlyph,
    OwnerAccent = Windows.UI.Color.FromArgb(255, 0, 122, 255),
    MorphView = new MyMorphView(),          // 实现 IMorphView
    CompactSize = new Windows.Foundation.Size(250, 40),
    ExpandedSize = new Windows.Foundation.Size(420, 150),
});

// 双视图：紧凑与展开是两个独立控件
Context.Island.SetContent(new IslandLiveContent
{
    CompactContent = compactPanel,
    ExpandedContent = expandedPanel,
    CompactSize = new Windows.Foundation.Size(250, 40),
    ExpandedSize = new Windows.Foundation.Size(420, 150),
});
```

| 属性 | 默认 | 说明 |
|------|------|------|
| `Priority` | 0 | 多个插件同时注册内容时，数值大者占据主岛，其余进展开后的队列 |
| `OwnerLabel` / `OwnerGlyph` / `OwnerAccent` | null | 归属标识（标签、字形、主题色） |
| `MorphView` | null | **推荐**，与 `Compact/ExpandedContent` 二选一 |
| `OnTap` | null | 内容被点击时回调 |
| `CompactSize` / `ExpandedSize` | 230×40 / 420×150 | 期望尺寸 |

**尺寸由宿主统一**：展开态所有内容同宽（取最大展开宽度）、队列卡片同高。视图要用自适应布局（Grid 的 `*` 行列、`Stretch` 对齐），不要假设一定拿到自己声明的精确尺寸。

需要"临时不占岛"时（例如用户关掉了显示开关）：`SetContent(null)`，插件本身继续运行。

## 6. IMorphView 与变形动画

```csharp
public interface IMorphView
{
    UIElement View { get; }
    void AnimateToExpanded(TimeSpan duration);
    void AnimateToCompact(TimeSpan duration);
}
```

宿主开始展开/收起时调用对应的 `AnimateTo*`，插件同步把内部元素动画到目标值。要点：

1. 用一个 `UserControl` 自己实现 `IMorphView`，`View => this`
2. **所有元素常驻可视树**（紧凑态 + 展开态都用同一棵树）
3. 紧凑态不显示的元素用 `Height = 0` + `Opacity = 0` 隐藏，**不要** `Visibility = Collapsed`（无法过渡）
4. 缓动曲线与宿主一致：`BackEase(EaseOut, Amplitude: 0.45)`，公式见 6.2
5. **形态动画必须逐帧直接给属性赋值，禁止用 `Storyboard`**（原因见 6.1）
6. `AnimateTo*` 可能在**任意时刻**被调用（"展开到一半又收起"、视图被放进展开队列的卡片里）：进度要在视图里自己累计（`_progress`），从当前位置接着走，而不是跳回起点

### 6.1 为什么禁止 Storyboard（会卡死）

动态加载的插件程序集里，**属性路径**动画 `Storyboard.SetTargetProperty(anim, "Height")` 解析不出属性所属类型，会在**动画 tick 上**抛：

```
COMException (0x800F1001): Invalid attribute value Unknown for property Height
```

- 异常发生在 tick 里，**调用点的 `try/catch` 拦不住**，会冒到宿主的未处理异常处理器
- 后果：岛体尺寸已经收回去了、插件的 `Height` 卡在中间值，整块内容错位且不再响应 hover —— 表现出来就是**"大岛变小之后卡死"**
- 会计入宿主的未处理异常计数，累计 5 次**插件被自动停用**
- 宿主内置视图（`WinIsland/Modules/Media`、`Battery`）能安全用 `Storyboard`，是因为它们在宿主程序集里、类型元数据可解析；插件侧不具备这个条件
- **与"用 XAML 还是纯代码建 UI"无关**：XAML 树更容易触发，纯代码树同样不能用

### 6.2 逐帧动画：核心写法（可直接用的完整版见模板 `MyPluginView.cs`）

```csharp
using Microsoft.UI.Dispatching;

public sealed class MyPluginView : UserControl, IMorphView
{
    private const double BackAmplitude = 0.45;          // 与宿主曲线一致
    private const double CompactIconSize = 16;
    private const double ExpandedIconSize = 22;
    private const double ExpandedDetailHeight = 44;

    private readonly FontIcon _icon;
    private readonly StackPanel _detail;

    private readonly DispatcherQueueTimer? _morphTimer;
    private DateTimeOffset _morphStart;
    private TimeSpan _morphDuration = TimeSpan.FromMilliseconds(333);
    private double _morphFrom;
    private double _morphTarget;
    private double _progress;                            // 0 = 紧凑，1 = 展开；反转时从这里接着走

    public MyPluginView(PluginManifest manifest)
    {
        _icon = new FontIcon { Glyph = manifest.IconGlyph ?? "\uE8BD", FontSize = CompactIconSize, ... };
        _detail = new StackPanel { Height = 0, Opacity = 0, Spacing = 4, ... };
        // ... 组装元素树 ...

        // 逐帧动画用的 UI 线程定时器；拿不到就退化成一帧切到终态，绝不留下中间值
        _morphTimer = DispatcherQueue?.CreateTimer();
        if (_morphTimer is not null)
        {
            _morphTimer.Interval = TimeSpan.FromMilliseconds(16);
            _morphTimer.IsRepeating = true;
            _morphTimer.Tick += (_, _) => OnMorphTick();
        }

        Unloaded += (_, _) => _morphTimer?.Stop();
    }

    public UIElement View => this;

    public void AnimateToExpanded(TimeSpan duration) => StartMorph(1, duration);

    public void AnimateToCompact(TimeSpan duration) => StartMorph(0, duration);

    private void StartMorph(double target, TimeSpan duration)
    {
        _morphFrom = _progress;
        _morphTarget = target;
        _morphDuration = duration > TimeSpan.Zero ? duration : TimeSpan.FromMilliseconds(1);
        _morphStart = DateTimeOffset.UtcNow;

        if (_morphTimer is null)
        {
            ApplyMorph(target);
            return;
        }

        _morphTimer.Start();
    }

    private void OnMorphTick()
    {
        var elapsed = (DateTimeOffset.UtcNow - _morphStart).TotalMilliseconds;
        var t = Math.Clamp(elapsed / Math.Max(1, _morphDuration.TotalMilliseconds), 0, 1);

        ApplyMorph(_morphFrom + (_morphTarget - _morphFrom) * BackEaseOut(t));

        if (t >= 1)
        {
            _morphTimer?.Stop();
            ApplyMorph(_morphTarget);       // 收尾锁定终态，避免残留中间值
        }
    }

    /// <summary>BackEase(EaseOut, A) = 1 + (A+1)(t-1)³ + A(t-1)²，与 XAML 那条曲线等价。</summary>
    private static double BackEaseOut(double t)
    {
        var d = t - 1;
        return 1 + (BackAmplitude + 1) * d * d * d + BackAmplitude * d * d;
    }

    /// <summary>把 0~1 的进度铺到各元素上。BackEase 会让 progress 略微越过 0/1，记得夹住。</summary>
    private void ApplyMorph(double progress)
    {
        _progress = progress;

        _detail.Height = Math.Max(0, ExpandedDetailHeight * progress);   // 不能为负
        _detail.Opacity = Math.Clamp(progress, 0, 1);

        _icon.FontSize = CompactIconSize + (ExpandedIconSize - CompactIconSize) * progress;
    }
}
```

另外两条：

- `Unloaded` 时把定时器停掉，别让它一直在后台跑。
- 定时器 / 每帧回调里的异常**同样直接进宿主的未处理异常处理器**（同样是 5 次停用），能在回调里兜住的错误就自己兜住（例如取数失败只记日志）。

> 参考实现（都在宿主源码仓库里）：`samples/WeatherIsland/WeatherIslandView.cs`（逐帧 + BackEase 0.45 的完整 MorphView）、`samples/XamlPlugin/Views/XamlPluginView.xaml.cs`（XAML 视图版）、宿主文档 `PLUGIN.md` §4。
> `WinIsland/Modules/Battery/BatteryIslandView.xaml.cs` 是 `Storyboard` 版，**只能参考视觉效果，不要照抄进插件**。

## 7. 临时消息 / 临时内容 / 设置页

```csharp
// 临时消息：显示几秒后自动恢复常驻内容
Context.Island.ShowMessage(new IslandMessage
{
    Title = "下载完成",
    Text = "文件已保存到 Downloads",
    Glyph = "\uE74C",                       // Segoe Fluent Icons 字形
    AccentColor = Windows.UI.Color.FromArgb(255, 0, 122, 255),
    Duration = TimeSpan.FromSeconds(3),
});

// 临时展示任意控件
Context.Island.Show(panel, new Windows.Foundation.Size(340, 92), TimeSpan.FromSeconds(5));

// 设置页：factory 每次进页面都会被调用，必须返回新实例
Context.Island.AddSettingsPage(new SettingsPageDescriptor(
    Manifest.Id, Manifest.Name, Manifest.IconGlyph ?? "\uE713",
    () => BuildSettingsPage(), order: 100));
```

`SettingsPageDescriptor(id, title, glyph, factory, order = 0)`，`order` 越大在设置窗口里的位置越靠后。

## 8. 设置、定时器、资源清理

```csharp
// 作用域化：Settings.Set("enabled", true) 实际存成 "<id>.enabled"
var enabled = Settings.Get("enabled", true);

// 监听某一个键（UI 线程回调；用 Register 包装后停用自动撤销）
Context.Register(Context.OnSettingsChanged("enabled", Refresh));

// 定时器：interval / 是否重复 / 回调（UI 线程；停用时自动停止）
Context.CreateTimer(TimeSpan.FromSeconds(1), repeat: true, OnTick);

// 其他需要释放的资源（事件订阅、原生句柄…）统一 Register，停用时自动 Dispose
Context.Register(new ActionDisposable(() => Log.Info("清理完成")));
```

设置键命名约定 `<id>.<key>`（SDK 自动加前缀）；插件的"禁用"状态由宿主存为 `plugin.<id>.disabled`，不要自己碰这个键。

## 9. 线程模型

- `OnInitializeAsync` / `OnShutdownAsync` / 设置页工厂 / `OnSettingsChanged` / `CreateTimer` 回调都在 **UI 线程**执行
- 采样、网络、文件等耗时工作放后台线程，算完用 `Context.RunOnUI(...)` 回 UI 更新
- 后台线程**绝不能**直接 `new UIElement`，必须回到 UI 线程再创建

## 10. 生命周期与状态

```
Discovered → Loaded → Active     正常运行
                     ↘ Disabled   被用户禁用（持久化为 plugin.<id>.disabled）
                     ↘ Faulted    初始化失败 / 运行期反复出错
卸载 → Unloading → 程序集请求卸载并验证回收
```

- `InitializeAsync` 在启用循环里**可能被多次调用**（禁用→启用、重新加载），代码要可重复执行
- 停用（`ShutdownAsync`）时宿主会兜底撤销：移除设置页、清空内容、停定时器、退订设置变更、收回临时消息
- 非 Active 状态调用岛 API 会被忽略并记日志（防止"停用后的幽灵行为"）
- 初始化超过 10 秒判定失败；单次会话 5 次未处理异常会自动停用插件

## 11. XAML 视图（可选路径，有坑）

动态加载的程序集不在应用的 `resources.pri` 里，`InitializeComponent()` 找不到自己的 XBF。需要 XAML 时：

```csharp
public sealed partial class MyView : UserControl, IMorphView
{
    public MyView() => PluginXaml.Load(this);   // 代替 InitializeComponent()
}
```

约束：
- XAML 文件与类同名，文件夹与命名空间一致（`MyPlugin.Views.MyView` ↔ `Views/MyView.xaml`）
- XAML 里只用框架类型，别引用插件自己的自定义控件
- **形态动画同样要遵守 6.1 的「逐帧赋值、禁止 Storyboard」**：这条与是否用 XAML 无关，XBF 树只是更容易触发的场景。逐帧写法见 6.2，XAML 版范本见 `samples/XamlPlugin/Views/XamlPluginView.xaml.cs`
- 图片等资源用 `Context.PluginDirectory` 拼绝对路径，不要用 `ms-appx:`
- 视图根元素保持透明背景

新手/第一版建议**直接用代码构建 UI**（更少坑），XAML 适合已有 XAML 经验的作者。

## 12. 依赖与打包规则

插件可以自带任意 NuGet 依赖和本机 DLL，宿主用 `AssemblyDependencyResolver` + `deps.json` 从插件目录解析：

1. 框架程序集（`System.*` / `Windows.*` / `WinRT.*`）与 `WinIsland.Core` → **始终绑定宿主**
2. 其它程序集：宿主目录里有同名 DLL → 用宿主那份；否则用插件目录里的
3. 所以：WinAppSDK/WinUI 运行时不要随插件分发；**自己的依赖必须真的复制到插件目录**（`CopyLocalLockFileAssemblies` + 打包脚本过滤规则）

4. **框架程序集的版本必须不高于宿主**：`Microsoft.Windows.SDK.NET`、`WinRT.Runtime` 这类投影只能由宿主提供（插件也不能把它们打进包），而 .NET **不允许向下绑定强命名程序集**。插件用比宿主新的 SDK 构建时，引用的投影版本高于宿主，加载直接失败：

   ```
   插件需要 Microsoft.Windows.SDK.NET 10.0.26100.86，宿主提供的是 10.0.26100.38：
   插件构建用的 .NET / Windows SDK 比宿主新，请用与宿主相同或更旧的 SDK 重新构建插件
   ```

   正式版宿主由 CI 用 `.NET 10.0.x` 构建，所以插件项目根目录（与 `.csproj` 同级）必须有 `global.json` 把 SDK 钉在 .NET 10 —— 模板自带这一份，别删：

   ```json
   {
     "sdk": {
       "version": "10.0.200",
       "rollForward": "latestFeature",
       "allowPrerelease": false
     }
   }
   ```

   没有它时 `dotnet build` 会挑机器上最高的 SDK（装了 .NET 11 / 预览版就挑它），于是出现"本地能跑、装到正式版宿主上就加载失败" —— 本地宿主和插件都是同一个新 SDK 构建的，正式版宿主不是。自检：在项目目录里 `dotnet --version` 必须是 `10.x`。

打包时**排除**这些（宿主自带或用不到）：
`WinIsland.Core.dll`、`Microsoft.WinUI.dll`、`Microsoft.Windows.SDK.NET.dll`、`WinRT.Runtime.dll`、`WebView2Loader.dll`、`Microsoft.WindowsAppRuntime*`、`Microsoft.Graphics.*.dll`、`Microsoft.InteractiveExperiences.*.dll`、`Microsoft.*.Projection.dll`、`*.pdb`
（`tools/pack-plugin.ps1` 和市场的 `submit-plugin.ps1` 会自动过滤；手动打包时自己注意）

## 13. 调试

```powershell
dotnet build          # 构建（配合 csproj 的 CopyToWinIsland 目标自动部署）
```

- 让插件重新加载：设置 → 插件管理 → 卡片上的「重新加载」，或先禁用再启用
- 日志：`%LocalAppData%\WinIsland\logs\plugin.<id>.log`，界面上每张卡片也有「查看日志」
- 手动改过 `plugins\` 里的文件后必须重新加载或重启 WinIsland 才生效

## 14. 1.x → 2.x 对照（识别旧资料）

| 1.x（已删除） | 2.x（现在） |
|-----|-----|
| `IIslandModule`（Id/DisplayName 写在代码里） | `IIslandPlugin` + `plugin.json` 提供元数据 |
| `[IslandPlugin(...)]` 特性 | 删除，改用 `plugin.json` |
| `InitializeAsync(IDynamicIslandApi api)` | `InitializeAsync(IPluginContext context)`（或重写 `OnInitializeAsync`） |
| `Api.SetLiveContent(Id, content)` | `Context.Island.SetContent(content)`（owner 由宿主绑定） |
| `Api.Settings`（全局键） | `Context.Settings`（自动加 `<id>.` 前缀） |
| `Api.Settings.Changed += …` | `Context.OnSettingsChanged(key, handler)` 或 `Register(...)` |
| `Api.Dispatcher.CreateTimer()` | `Context.CreateTimer(...)`（自动随停用释放） |
| 根目录散装 `*.dll` | `plugins/<id>/` 目录或 `.lwp` 包 |
| `luolan.winland.Core` NuGet 1.x | 不兼容；用 2.x（`>= 2.0.0`）的包或源码 |

## 15. 设置项与刷新：两个"改了没反应"的坑

设置页看起来最简单，却是"改了不生效"类 bug 的重灾区。下面两条是宿主样例（`samples/WeatherIsland`）踩过并修好的真实问题。

### 15.1 设置值要在"动作发生前"提交，别只等 LostFocus

只挂 `LostFocus` 保存设置是最常见的写法：

```csharp
_cityBox.LostFocus += (_, _) => SaveCity();   // 只在离开焦点时保存
```

问题：用户改完城市**直接点「立即刷新」**——按钮点击先发生，`LostFocus` 还没触发，于是拿**旧城市**去刷了。同理还有两个隐藏坑：键盘用户只按回车不点别处；`ComboBox` 在构建页面时会触发一次 `SelectionChanged`（会把默认值写回设置）。

正确做法是把三个提交点都覆盖：

```csharp
// 1) 页面里带动作的按钮：先提交，再干活
private async void OnRefreshClick(object sender, RoutedEventArgs e)
{
    CommitEditors();                 // ← 把输入框里还没保存的值写进设置
    ...                              // 再做刷新
}

// 2) 控件自己的提交时机：失焦 + 回车（键盘用户）
_cityBox.LostFocus += (_, _) => CommitEditors();
_cityBox.KeyDown += (_, e) =>
{
    if (e.Key == Windows.System.VirtualKey.Enter) CommitEditors();
};

// 3) 页面初次填充时不要回写设置
private bool _loading = true;        // 构造函数最后一行置 false
private void CommitEditors()
{
    if (_loading) return;

    var text = (_cityBox.Text ?? string.Empty).Trim();
    if (string.Equals(text, _settings.Get("city", "北京"), StringComparison.Ordinal)) return;   // 值没变就别写
    _settings.Set("city", text);
}
```

- `Settings.Set` 会触发 `OnSettingsChanged`，**值没变就别写**，否则每次失焦都会白触发一次刷新。
- 异步取数**每次都重新读设置**，不要用 `OnInitializeAsync` 时缓存的字段 —— 这样即使设置变更回调还没跑，动作用的也是新值。
- 结果要回推给还开着的设置页，否则状态行停在上一座城市：

```csharp
// 插件侧（在 UI 线程触发）
public event Action<WeatherSnapshot>? SnapshotApplied;
...
Context.RunOnUI(() =>
{
    _view.Apply(applied);

    try { SnapshotApplied?.Invoke(applied); }                 // 监听者出错不能影响宿主
    catch (Exception ex) { Log.Warn($"设置页状态同步失败（已忽略）：{ex.Message}"); }
});

// 设置页侧：订阅 + 页面卸载时退订（别让插件一直握着这个页面）
_plugin.SnapshotApplied += OnSnapshotApplied;
Unloaded += (_, _) => _plugin.SnapshotApplied -= OnSnapshotApplied;

private void OnSnapshotApplied(WeatherSnapshot snapshot)
{
    // 回填输入框前先看焦点，别把用户正在输入的内容冲掉
    if (snapshot.HasData && _cityBox.FocusState == FocusState.Unfocused)
    {
        _cityBox.Text = snapshot.Place;
    }
}
```

### 15.2 刷新请求要排队，不许"忙就丢弃"

定时刷新 + 手动刷新 + 设置变更三处都会触发取数，**并发是常态**。"忙就直接 return"的单飞模式会**静默吞掉**后来的请求：

```csharp
// 错：并发时后来的请求被无声丢弃，日志里连一条记录都没有
if (Interlocked.Exchange(ref _busy, 1) == 1) return;
```

典型事故：用户改完城市点「立即刷新」→ 手动刷新正握着锁 → 「城市变更」那次刷新被丢掉 → 界面停在旧城市，排查时日志里**一条城市变更都没有**。

正确做法：同一时刻只跑一次，但后来的请求**排队等待、绝不丢弃**：

```csharp
private readonly SemaphoreSlim _refreshGate = new(1, 1);   // 串行化：排队而不是丢弃
private int _refreshing;
private bool _stopped;

private async Task<WeatherSnapshot> RefreshAsync(string reason)
{
    if (_stopped) return _snapshot;

    await _refreshGate.WaitAsync().ConfigureAwait(false);
    Interlocked.Exchange(ref _refreshing, 1);
    try
    {
        return await FetchOnceAsync(reason).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        Log.Error("取数出错（已忽略）", ex);      // 异步方法里的异常必须自己兜住
        return _snapshot;
    }
    finally
    {
        Interlocked.Exchange(ref _refreshing, 0);
        _refreshGate.Release();
    }
}

private async Task<WeatherSnapshot> FetchOnceAsync(string reason)
{
    if (_stopped) return _snapshot;

    var city = ReadCity();                        // ← 真正的取数入口重新读设置
    Log.Info($"开始取数（{reason}，城市 {city}）");
    ...
}
```

- 每次刷新都带一个 `reason`（`首次加载` / `定时刷新` / `手动刷新` / `城市变更`）并写日志：出问题时一眼能看出是哪条路径没跑到。
- 停用（`ShutdownAsync`）时把 `_stopped` 置 true 并在入口 return，避免停用后还在发请求（否则日志里会出现「已忽略调用」）。
- 后台线程算完必须 `Context.RunOnUI(...)` 回到 UI 线程再碰控件。
- 取数失败保留上一次的数据继续显示，比清空界面更友好。

## 16. 超级展开（Spotlight 聚光卡）

大岛装不下的信息**不要继续往岛里塞元素** —— 让点击打开「超级展开」：一张居中的大卡片从岛体位置
**带倾角飞入并放大**，点卡片外区域或按 `Esc` 反向动画收回。**何时打开由插件决定**，卡片尺寸与内容也由插件决定。

```csharp
private MySpotlightView? _detail;

private void OpenDetail()
{
    _detail ??= new MySpotlightView();                 // 必须是独立可视树（见下面的坑）
    Context.Island.OpenSpotlight(new IslandSpotlight
    {
        Content = _detail,
        Size = new Windows.Foundation.Size(720, 460),   // 期望尺寸（DIP）
        OnClosed = () => _detail?.OnHostClosed(),
    });
}

// 想自己收起（比如数据源没了）：Context.Island.CloseSpotlight();
```

卡片视图就是普通控件树（`UserControl` 或代码构建都行），**不需要实现 `IMorphView`**：飞入飞回、遮罩、圆角、层级
全由宿主负责；视图只管「最终形态长什么样」，并自己管好定时器：

```csharp
public sealed class MySpotlightView : UserControl
{
    public MySpotlightView()
    {
        Loaded += (_, _) => _timer.Start();      // 宿主把内容挂上可视树时
        Unloaded += (_, _) => _timer.Stop();     // 宿主收起卡片、把内容卸下时
    }

    public void OnHostClosed() { /* 取消网络请求之类的收尾 */ }
}
```

| 成员 | 说明 |
|------|------|
| `Content` | 卡片内容。**必须是独立于岛视图的另一棵树**：每个窗口一棵树，把岛视图那个 `UIElement` 传进来会白屏或抛异常 |
| `Size` | 期望尺寸（DIP）。宿主居中摆放并夹到显示器工作区 92% 以内，请用自适应布局、别假设精确尺寸 |
| `OnClosed` | 关闭回调（点卡片外 / `Esc` / 自己调 `CloseSpotlight` / 插件被停用 / 被别的插件替换），宿主已做异常保护 |

四条硬性规则：

1. **需要展示更多信息就用聚光卡**，不要在岛里继续塞元素（岛体尺寸是宿主统一管的，塞多了会把所有插件的展开观感一起拖垮）。
2. **点击岛体 = 打开聚光卡**（在 `OnTap` 里调 `OpenSpotlight`）；不要再用点击去做「跳到别的应用」这类事 —— 那是聚光卡内部按钮该干的活。宿主会识别交互控件：点在按钮/滑块上不会触发 `OnTap`，但你**自绘的可拖动控件**（比如进度条）要在自己的 `Tapped` 里写 `e.Handled = true`，否则拖一下就会顺带弹出聚光卡。
3. **同一时刻只有一张卡**：别的插件再开就替换（你会先收到 `OnClosed`）；插件停用/卸载时宿主自动收起，不用自己清理。
4. `plugin.json` 的 `min_host_version` 写 `"2.1.0"`（旧宿主没有这些 API，调用会抛 `MissingMethodException` 并被记成插件异常）；`api_version` 仍然是 `2`。

## 17. 文件投放（把文件 / 文本 / 图片拖到岛上）

用户从资源管理器（或浏览器、编辑器）把东西拖到岛上时，岛会展开成一排**投放卡片**，
每张卡片是一个"松手就执行"的动作 —— 拖到卡片上松手就调用你的 `Handler`。
**展示、命中、边缘自动滚动、悬停放大、系统拖拽气泡全由宿主接管**，插件只负责"拿到内容之后干什么"。

```csharp
protected override Task OnInitializeAsync()
{
    Context.Island.AddDropTarget(new IslandDropTarget
    {
        Id = "add-to-playlist",                     // 插件内唯一；重复注册同一个 Id 是覆盖语义
        Title = "加入播放列表",                      // 卡片标题（卡片只有 72px 宽，越短越好）
        Glyph = "\uE8C8",                           // Segoe Fluent Icons / Segoe MDL2 Assets 字形
        Hint = "加进当前列表",                       // 可选：显示在系统拖拽气泡里（卡片上放不下）
        AccentColor = Windows.UI.Color.FromArgb(255, 0x4C, 0xC2, 0xFF),
        Order = 100,                                // 升序；宿主内置动作是 900+，插件默认 0 排在前面
        Kinds = IslandDropKind.Files,               // 接受哪些载荷（默认只收文件）
        Extensions = new[] { ".mp3", ".flac" },     // 可选：文件的扩展名白名单（不填 = 全收）
        Handler = async context =>
        {
            foreach (var path in context.Paths) await AddAsync(path);
            return $"已加入 {context.Paths.Count} 首";   // 返回文案 → 宿主弹一条临时消息（null = 不提示）
        },
    });

    return Task.CompletedTask;
}
```

### 17.1 载荷：一次只带一种

| `Kind` | 用户怎么拖出来 | `IslandDropContext` 里有值的是 |
|---|---|---|
| `IslandDropKind.Files` | 资源管理器里拖文件 / 文件夹 | `Paths`（完整路径）、`Names`（文件名，与 Paths 一一对应） |
| `IslandDropKind.Text` | 浏览器 / 编辑器里选中一段文字或链接拖过来 | `Text` |
| `IslandDropKind.Image` | 浏览器里拖图片、截图工具里拖图 | `ImageBytes`（**源格式**的原始字节，通常是 PNG / JPEG） |

判定顺序是 **文件 > 图片 > 文本**（从浏览器拖图片时往往同时带文本＝图片地址，那种情况按图片处理）。
`ItemCount` / `IsSingle` 描述"这一批有多少项"：文件是路径条数，文本 / 图片算 1。

### 17.2 三种过滤：Kinds、Extensions、全收

```csharp
Kinds = IslandDropKind.Files | IslandDropKind.Image,   // 文件和图片都收
Extensions = new[] { ".png", ".jpg" },                 // 只对文件载荷生效（只收图片文件）
Kinds = IslandDropKind.All,                            // 三种都收，按 context.Kind 分支处理
```

- **不匹配的卡片根本不出现**（不是变暗）：面板只展示"能对这份载荷做什么"；一张都匹配不上时摘要写「没有卡片能接收它」。
- 用户拖得很快（载荷还没读完就松手）时卡片会先全亮，宿主在松手瞬间按真实载荷**复核一次**，不匹配就当落空 —— 不会误触发你的 `Handler`。
- 文件夹没有扩展名，所以只声明了 `Extensions` 的卡片对文件夹一律不收。

### 17.3 必须知道的规则

1. **只在需要"接收外部内容"时才注册投放目标**：它不是常驻 UI，只在拖拽期间出现。
2. **`Handler` 在 UI 线程被调用**（界面状态可以放心直接改）；耗时活儿自己 `await` / `RunOnUI` 编组 —— 松手那一刻投放面板就已经收回了，宿主不会因为你慢而卡住。
3. **`Handler` 抛异常不影响宿主**：宿主包了守卫，只记日志并计入"累计 5 次未处理异常自动停用"。
4. **插件停用 / 卸载时卡片自动消失**（`PluginScope` 兜底），不需要自己清理；运行期想换一批卡片就再调一次 `AddDropTarget`（同 Id 覆盖）或 `RemoveDropTarget(id)`。
5. **图片有 32MB 上限**：超过或读不出来时这次投放等于落空，日志里会写明原因。
6. **`Order` 决定卡片顺序**：插件默认 0，排在宿主内置动作（打开 / 所在位置 / 复制路径 / 复制文本 / 保存图片，900+）前面。
7. **用户可以在「设置 → 通用 → 文件投放」里关掉整个功能**：关掉后岛对拖放完全无感，这不是插件的 bug。
8. `plugin.json` 的 `min_host_version` 写 `"2.2.0"`（旧宿主没有这些 API，调用会抛 `MissingMethodException` 并被记成插件异常）；`api_version` 仍然是 `2`。

## 18. 主题与配色（浅色 / 深色适配）

岛的 Fluent 外观**跟随系统明暗**（设置 → 个性化 → 颜色 → 「默认应用模式」），系统在进程存活期间切换时会**当场**生效；Apple 外观恒为深色黑胶囊。所以插件视图的配色必须两套都能看：写死白色 → 浅色主题下白字压白底；浅色时建好、之后不重刷 → 系统切深色后变成深色岛上的黑字。

### 18.1 XAML 视图：全部交给 ThemeResource（零代码）

文字直接用系统画刷，岛体换主题时它们自己跟着换：

```xml
<TextBlock Text="标题" Foreground="{ThemeResource TextFillColorPrimaryBrush}" />
<TextBlock Text="说明" Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
<TextBlock Text="提示" Foreground="{ThemeResource TextFillColorTertiaryBrush}" />
```

自定义的中性色（灰底、分隔线、占位块）自己给两套：

```xml
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Light">
                <SolidColorBrush x:Key="MyNeutralFillBrush" Color="#26000000" />
            </ResourceDictionary>
            <ResourceDictionary x:Key="Dark">
                <SolidColorBrush x:Key="MyNeutralFillBrush" Color="#26FFFFFF" />
            </ResourceDictionary>
        </ResourceDictionary.ThemeDictionaries>
    </ResourceDictionary>
</UserControl.Resources>

<Border Background="{ThemeResource MyNeutralFillBrush}" />
```

### 18.2 代码搭的视图：`IIslandTheme`

代码里取不到"当前元素主题"，必须由插件把它传进视图。`IIslandTheme` 只有两个成员：

| 成员 | 说明 |
|------|------|
| `bool IsLight` | 岛体当前是否浅色（Apple 风格恒为 `false`） |
| `event Action Changed` | 主题变化（UI 线程触发）：在这里重刷配色 |

推荐写法（模板 `assets/plugin-template/MyPluginView.cs` 就是这一份）：中性色做成**共享画刷字段**，换主题时只改它们的 `Color` —— 所有用到这支画刷的元素当场跟着变，不用重建视图、也不用逐元素重设 `Foreground`。

```csharp
public MyPluginView(PluginManifest manifest, IIslandTheme theme)
{
    _theme = theme;
    ApplyThemeColors();
    _theme.Changed += ApplyThemeColors;      // 岛体换主题
    ...
    _title.Foreground = _textBrush;          // 用共享画刷，别 new 一支写死的
}

private void ApplyThemeColors()
{
    _textBrush.Color = Neutral(255);
    _faintBrush.Color = Neutral(160);
}

/// <summary>中性色：岛体深色时白色系，浅色（Fluent + 浅色系统）时黑色系。</summary>
private Windows.UI.Color Neutral(byte alpha) => _theme.IsLight
    ? Windows.UI.Color.FromArgb(alpha, 0, 0, 0)
    : Windows.UI.Color.FromArgb(alpha, 255, 255, 255);
```

聚光卡（§16）是另一棵可视树，同样要接 `IIslandTheme`：卡片本身就跟着岛体一起明暗切换，白字同样会看不见。

### 18.3 规则

1. **不要自己读注册表 / 系统主题**：`IsLight` 说的是**岛体**的明暗，不是系统主题 —— 岛体可能是"系统浅色 + Apple 深色胶囊"这种组合，只有宿主知道该用哪套。
2. **不要用应用级主题代替**：`Application.Current.RequestedTheme` 与应用资源里的画刷是"设置窗"的主题，和岛体不是一回事。
3. `theme.Changed` 在 UI 线程触发，可以直接改 UI；回调里抛异常只记日志并计入"累计 5 次未处理异常自动停用"。
4. 视图活得越久越要重刷：宿主会在插件停用时撤销一切，但**主题变化不会重建你的视图**。
5. 用了 `Context.Theme` 的插件，`plugin.json` 的 `min_host_version` 写 `"2.3.0"`；`api_version` 仍然是 `2`。
6. 自查：把系统主题切一遍（浅色↔深色），岛上的文字、灰底、分隔线都得跟着变。
