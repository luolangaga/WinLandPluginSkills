using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinIsland.Core;

namespace MyPlugin;

/// <summary>
/// 岛上的视图：同一棵可视树同时承载「小岛（紧凑态）」和「大岛（展开态）」，
/// 宿主切换形态时调用 AnimateToExpanded / AnimateToCompact 让内部元素同步变形。
///
/// 四条要点：
///   1. 所有元素（含只在展开态出现的）一开始就常驻可视树；
///   2. 隐藏用 Height = 0 + Opacity = 0，不要用 Visibility = Collapsed（无法过渡）；
///   3. 形态动画逐帧直接赋值，**不要用 Storyboard**（见 StartMorph 的注释）；
///   4. 缓动与宿主保持一致：BackEase(EaseOut, Amplitude: 0.45)。
/// </summary>
public sealed class MyPluginView : UserControl, IMorphView
{
    private const double CompactIconSize = 16;
    private const double ExpandedIconSize = 22;
    private const double CompactTitleSize = 13;
    private const double ExpandedTitleSize = 15;
    private const double ExpandedDetailHeight = 44;

    /// <summary>与宿主内置视图同一条 BackEase 曲线的幅度，手感保持一致。</summary>
    private const double BackAmplitude = 0.45;

    private readonly FontIcon _icon;
    private readonly TextBlock _title;
    private readonly TextBlock _status;
    private readonly StackPanel _detail;

    private readonly DispatcherQueueTimer? _morphTimer;
    private DateTimeOffset _morphStart;
    private TimeSpan _morphDuration = TimeSpan.FromMilliseconds(333);
    private double _morphFrom;
    private double _morphTarget;

    /// <summary>当前形态进度：0 = 紧凑态，1 = 展开态。反转动画时从这里接着走。</summary>
    private double _progress;

    public MyPluginView(PluginManifest manifest)
    {
        _icon = new FontIcon
        {
            Glyph = manifest.IconGlyph ?? "\uE8BD",
            FontSize = CompactIconSize,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 194, 255)),
        };

        _title = new TextBlock
        {
            Text = manifest.Name,
            FontSize = CompactTitleSize,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
        };

        _status = new TextBlock
        {
            Text = "已就绪",
            FontSize = 13,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(210, 255, 255, 255)),
        };

        // 展开态才显示的内容：Height/Opacity 归零藏起来（元素仍在可视树里）
        _detail = new StackPanel { Height = 0, Opacity = 0, Spacing = 4 };
        _detail.Children.Add(_status);
        _detail.Children.Add(new TextBlock
        {
            Text = manifest.Description ?? "",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 255, 255, 255)),
        });

        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        header.Children.Add(_icon);
        header.Children.Add(_title);

        var root = new StackPanel
        {
            Spacing = 8,
            Padding = new Thickness(18, 10, 18, 10),
            VerticalAlignment = VerticalAlignment.Center,
        };
        root.Children.Add(header);
        root.Children.Add(_detail);

        Content = root;

        // 逐帧动画用的 UI 线程定时器；拿不到就退化成「直接切终态」，绝不留下中间值
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

    /// <summary>更新展开态里的状态文字（插件定时器调用）。</summary>
    public void SetStatus(string text) => _status.Text = text;

    public void AnimateToExpanded(TimeSpan duration) => StartMorph(expanded: true, duration);

    public void AnimateToCompact(TimeSpan duration) => StartMorph(expanded: false, duration);

    /// <summary>
    /// 形态动画走「逐帧属性赋值」，刻意不用 Storyboard。
    ///
    /// 原因：动态加载的插件程序集里，属性路径动画（Storyboard.SetTargetProperty 的 "Height"）
    /// 解析不出类型信息，会在动画 tick 上抛
    /// COMException (0x800F1001): Invalid attribute value Unknown for property Height。
    /// 这个异常发生在 tick 里，调用点的 try/catch 拦不住，会冒到宿主的未处理异常处理器，
    /// 结果是岛体尺寸收回去了、插件的 Height 却卡在中间值，整块内容错位且不再响应 hover。
    /// 宿主内置视图能安全使用 Storyboard，是因为它们在宿主程序集里；插件侧必须逐帧赋值。
    /// </summary>
    private void StartMorph(bool expanded, TimeSpan duration)
    {
        var target = expanded ? 1d : 0d;

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
        var durationMs = Math.Max(1, _morphDuration.TotalMilliseconds);
        var t = Math.Clamp(elapsed / durationMs, 0, 1);

        ApplyMorph(_morphFrom + (_morphTarget - _morphFrom) * BackEaseOut(t));

        if (t >= 1)
        {
            _morphTimer?.Stop();
            ApplyMorph(_morphTarget);   // 收尾时锁定终态，避免残留中间值
        }
    }

    /// <summary>BackEase(EaseOut, A) = 1 + (A+1)(t-1)³ + A(t-1)²，与 XAML 那条曲线等价。</summary>
    private static double BackEaseOut(double t)
    {
        var d = t - 1;
        return 1 + (BackAmplitude + 1) * d * d * d + BackAmplitude * d * d;
    }

    /// <summary>把 0~1 的形态进度铺到各元素上。progress 会因 BackEase 略微越过 0/1。</summary>
    private void ApplyMorph(double progress)
    {
        _progress = progress;

        // Height 不能为负（BackEase 在终点附近会把曲线推到目标值以下）
        _detail.Height = Math.Max(0, ExpandedDetailHeight * progress);
        _detail.Opacity = Math.Clamp(progress, 0, 1);

        var iconSize = CompactIconSize + (ExpandedIconSize - CompactIconSize) * progress;
        _icon.FontSize = iconSize;

        var titleSize = CompactTitleSize + (ExpandedTitleSize - CompactTitleSize) * progress;
        _title.FontSize = titleSize;
    }
}
