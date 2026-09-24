using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinIsland.Core;

namespace MyPlugin;

/// <summary>
/// 插件入口。一个插件包有且只有一个 IIslandPlugin 实现，
/// 必须是 public sealed 且有公共无参构造函数，否则宿主会拒绝加载。
/// </summary>
public sealed class MyPlugin : IslandPluginBase
{
    private const string DefaultText = "已就绪";

    private MyPluginView _view = null!;
    private MyPluginSpotlightView? _spotlightView;
    private IslandLiveContent _content = null!;
    private int _seconds;

    protected override Task OnInitializeAsync()
    {
        Log.Info($"启动：{Manifest.Id} {Manifest.Version}，插件目录 {PluginDirectory}");

        _view = new MyPluginView(Manifest);

        // 注册常驻内容：小岛/大岛的尺寸和动画由宿主统一管理
        _content = new IslandLiveContent
        {
            Priority = Settings.Get("priority", 10),
            OwnerLabel = Manifest.Name,
            OwnerGlyph = Manifest.IconGlyph,
            OwnerAccent = Windows.UI.Color.FromArgb(255, 0, 122, 255),
            MorphView = _view,
            CompactSize = new Windows.Foundation.Size(240, 40),
            ExpandedSize = new Windows.Foundation.Size(400, 120),
            OnTap = OpenSpotlight,      // 点击岛体 = 打开「超级展开」聚光卡
        };

        Context.Island.AddSettingsPage(new SettingsPageDescriptor(
            Manifest.Id, Manifest.Name, Manifest.IconGlyph ?? "\uE713", BuildSettingsPage, order: 100));

        // 设置页里的开关实时生效
        Context.Register(Context.OnSettingsChanged("enabled", () =>
        {
            SetContent(Settings.Get("enabled", true) ? _content : null);
        }));

        // 设置页里改的显示文字实时生效
        Context.Register(Context.OnSettingsChanged("text", () => RefreshNow("设置变更")));

        if (Settings.Get("enabled", true))
        {
            SetContent(_content);
        }

        // UI 线程定时器：插件停用时宿主会自动停止它
        Context.CreateTimer(TimeSpan.FromSeconds(1), repeat: true, OnTick);
        return Task.CompletedTask;
    }

    protected override Task OnShutdownAsync()
    {
        Log.Info("已停用");
        SetContent(null);
        return Task.CompletedTask;
    }

    private void OnTick()
    {
        _seconds++;
        RefreshNow("定时刷新");
    }

    /// <summary>
    /// 刷新小岛内容。两个要点（细节见 references/sdk-api.md §15）：
    ///   1. 动作之前先把输入框里尚未提交的值写进设置 —— 按钮点击早于 LostFocus；
    ///   2. 每次刷新都重新读设置，不要用初始化时缓存的字段。
    ///
    /// 如果刷新要走网络（async），按 §15.2 用 SemaphoreSlim 排队串行化，
    /// **不要**用「忙就 return」的单飞模式：那会把并发请求静默丢掉。
    /// </summary>
    private void RefreshNow(string reason)
    {
        var text = Settings.Get("text", DefaultText);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = $"已运行 {_seconds} 秒";
        }

        Log.Info($"刷新（{reason}）：{text}");
        _view.SetStatus(text);
    }

    /// <summary>把输入框里的值写进设置。值没变就不写，避免多余的通知与刷新。</summary>
    private void CommitText(TextBox box)
    {
        var text = (box.Text ?? string.Empty).Trim();
        if (string.Equals(text, Settings.Get("text", DefaultText), StringComparison.Ordinal)) return;

        Settings.Set("text", text);   // 会触发 OnSettingsChanged("text") → RefreshNow("设置变更")
    }

    /// <summary>
    /// 打开「超级展开」聚光卡：卡片尺寸、内容、何时打开都由插件决定；
    /// 飞入/飞回动画、遮罩、点卡片外与 Esc 收起由宿主负责。
    /// 视图必须独立于岛视图（每个窗口一棵树），实例可以复用。
    /// </summary>
    private void OpenSpotlight()
    {
        _spotlightView ??= new MyPluginSpotlightView(Manifest);

        Context.Island.OpenSpotlight(new IslandSpotlight
        {
            Content = _spotlightView,
            Size = new Windows.Foundation.Size(720, 460),   // 期望尺寸（DIP），宿主会夹到工作区内
            OnClosed = () => _spotlightView?.OnHostClosed(),
        });
    }

    // ---- 文件投放（可选能力，完整说明见 references/sdk-api.md §17）----
    //
    // 想让插件"接收"用户拖进来的东西时用它：从资源管理器 / 浏览器把文件、文本、图片拖到岛上，
    // 岛会展开成一排投放卡片，拖到你这张卡片上松手就执行 Handler。
    // 注意：用了这个 API，plugin.json 的 min_host_version 必须提到 "2.2.0"（旧宿主没有它）。
    //
    // private void RegisterDropTarget()
    // {
    //     Context.Island.AddDropTarget(new IslandDropTarget
    //     {
    //         Id = "add-to-list",                                    // 插件内唯一；重复注册同一个 Id 是覆盖
    //         Title = "加入列表",                                     // 卡片只有 72px 宽，标题越短越好
    //         Glyph = "\uE8C8",
    //         Hint = "加进当前列表",                                  // 显示在系统拖拽气泡里（卡片上放不下）
    //         Order = 100,                                           // 升序；宿主内置动作是 900+
    //         Kinds = IslandDropKind.Files | IslandDropKind.Text,    // 收哪些载荷（默认只收文件）
    //         Extensions = new[] { ".mp3", ".flac" },                // 可选，只对文件载荷生效
    //         Handler = context =>
    //         {
    //             var what = context.Kind == IslandDropKind.Files
    //                 ? string.Join("、", context.Names)
    //                 : context.Text;
    //
    //             return Task.FromResult<string?>($"收到：{what}");   // 返回的文案由宿主弹成一条提示
    //         },
    //     });
    // }
    //
    // 在 OnInitializeAsync 里调一次 RegisterDropTarget() 即可；插件停用 / 卸载时卡片自动消失。

    /// <summary>设置页工厂：每次用户点进页面都会调用一次，必须返回新实例。</summary>
    private UIElement BuildSettingsPage()
    {
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(new TextBlock
        {
            Text = Manifest.Name,
            Style = (Style)Application.Current.Resources["TitleTextBlockStyle"],
        });

        var toggle = new ToggleSwitch
        {
            Header = "在灵动岛上显示",
            IsOn = Settings.Get("enabled", true),
        };
        toggle.Toggled += (_, _) => Settings.Set("enabled", toggle.IsOn);

        panel.Children.Add(new Border
        {
            Style = (Style)Application.Current.Resources["SettingsCardStyle"],
            Child = toggle,
        });

        // 文本设置项：三个提交点都要覆盖，别只等 LostFocus（见 references/sdk-api.md §15.1）
        var textBox = new TextBox
        {
            Header = "展开后显示的文字",
            Text = Settings.Get("text", DefaultText),
            PlaceholderText = $"留空则显示运行时长，默认「{DefaultText}」",
            MinWidth = 280,
        };
        textBox.LostFocus += (_, _) => CommitText(textBox);
        textBox.KeyDown += (_, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Enter) CommitText(textBox);
        };

        var refreshButton = new Button { Content = "立即刷新" };
        refreshButton.Click += (_, _) =>
        {
            // 动作之前先提交：否则「改完直接点按钮」用的还是旧值（按钮点击早于 LostFocus）
            CommitText(textBox);
            RefreshNow("手动刷新");
        };

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        actions.Children.Add(refreshButton);

        panel.Children.Add(new Border
        {
            Style = (Style)Application.Current.Resources["SettingsCardStyle"],
            Child = new StackPanel { Spacing = 8, Children = { textBox, actions } },
        });

        var testButton = new Button { Content = "发一条测试消息" };
        testButton.Click += (_, _) => Context.Island.ShowMessage(new IslandMessage
        {
            Title = Manifest.Name,
            Text = "这是来自插件设置页的测试消息",
            Glyph = Manifest.IconGlyph ?? "\uE8BD",
            Duration = TimeSpan.FromSeconds(3),
        });
        panel.Children.Add(testButton);

        return panel;
    }
}
