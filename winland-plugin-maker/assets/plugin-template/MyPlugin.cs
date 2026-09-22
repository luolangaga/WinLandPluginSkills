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
        _view.SetStatus($"已运行 {_seconds} 秒");
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
