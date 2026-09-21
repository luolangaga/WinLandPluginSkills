# WinIsland 插件 SDK 2.0 参考（插件作者版）

写代码前通读本文。所有类型都在 `WinIsland.Core` 命名空间下。**2.0 与 1.x 不兼容**，文末有对照表，看到旧写法一律作废。

## 1. 工程怎么引用 SDK

SDK 2.0 目前以源码形式提供（`WinIsland.Core` 项目）。推荐把插件项目放在 WinIsland 源码仓库的 `samples\` 下，用 `ProjectReference`：

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
4. 缓动统一 `new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.45 }`
5. 动画用 `Storyboard` + `Storyboard.SetTarget/SetTargetProperty`（代码构建的元素树可用）；`Height`/`Width`/`FontSize` 这类依赖属性需要 `EnableDependentAnimation = true`

```csharp
public void AnimateToExpanded(TimeSpan duration)
{
    var storyboard = new Storyboard();
    var easing = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.45 };
    storyboard.Children.Add(Anim(_detail, "Height", _detail.Height, 46, duration, easing));
    storyboard.Children.Add(Anim(_detail, "Opacity", _detail.Opacity, 1, duration, easing));
    storyboard.Begin();
}

private static DoubleAnimation Anim(DependencyObject target, string property,
    double from, double to, TimeSpan duration, EasingFunctionBase easing)
{
    var animation = new DoubleAnimation
    {
        From = from,
        To = to,
        Duration = new Duration(duration),
        EasingFunction = easing,
        EnableDependentAnimation = true,
    };
    Storyboard.SetTarget(animation, target);
    Storyboard.SetTargetProperty(animation, property);
    return animation;
}
```

> 没有灵感时，去抄宿主仓库里的生产级实现：`WinIsland/Modules/Battery/BatteryIslandView.xaml.cs`（Storyboard + BackEase 的完整 MorphView）。

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
- **不要用 Storyboard 做形态动画**：属性路径动画在动态加载的 XBF 树上会报 `Invalid attribute value Unknown for property Height`（E_XAMLPARSEFAILED，会打断宿主状态机）。改用逐帧属性赋值——完整范本见 `samples/XamlPlugin/Views/XamlPluginView.xaml.cs`
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
| `luolan.winland.Core` NuGet 1.x | 不兼容；用源码里的 `WinIsland.Core`（api_version 2） |
