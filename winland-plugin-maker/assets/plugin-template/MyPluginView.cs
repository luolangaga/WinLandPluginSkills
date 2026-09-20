using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using WinIsland.Core;

namespace MyPlugin;

/// <summary>
/// 岛上的视图：同一棵可视树同时承载「小岛（紧凑态）」和「大岛（展开态）」，
/// 宿主切换形态时调用 AnimateToExpanded / AnimateToCompact 让内部元素同步变形。
///
/// 三条要点：
///   1. 所有元素（含只在展开态出现的）一开始就常驻可视树；
///   2. 隐藏用 Height = 0 + Opacity = 0，不要用 Visibility = Collapsed（无法过渡）；
///   3. 缓动与宿主保持一致的 BackEase(EaseOut, 0.45)。
/// </summary>
public sealed class MyPluginView : UserControl, IMorphView
{
    private const double CompactIconSize = 16;
    private const double ExpandedIconSize = 22;
    private const double CompactTitleSize = 13;
    private const double ExpandedTitleSize = 15;
    private const double ExpandedDetailHeight = 44;

    private readonly FontIcon _icon;
    private readonly TextBlock _title;
    private readonly TextBlock _status;
    private readonly StackPanel _detail;

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
    }

    public UIElement View => this;

    /// <summary>更新展开态里的状态文字（插件每秒调用一次）。</summary>
    public void SetStatus(string text) => _status.Text = text;

    public void AnimateToExpanded(TimeSpan duration) => AnimateTo(expanded: true, duration);

    public void AnimateToCompact(TimeSpan duration) => AnimateTo(expanded: false, duration);

    private void AnimateTo(bool expanded, TimeSpan duration)
    {
        var easing = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.45 };
        var storyboard = new Storyboard();

        storyboard.Children.Add(Anim(_icon, "FontSize", _icon.FontSize,
            expanded ? ExpandedIconSize : CompactIconSize, duration, easing));
        storyboard.Children.Add(Anim(_title, "FontSize", _title.FontSize,
            expanded ? ExpandedTitleSize : CompactTitleSize, duration, easing));
        storyboard.Children.Add(Anim(_detail, "Height", _detail.Height,
            expanded ? ExpandedDetailHeight : 0, duration, easing));
        storyboard.Children.Add(Anim(_detail, "Opacity", _detail.Opacity,
            expanded ? 1 : 0, duration, easing));

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
            EnableDependentAnimation = true,   // Height/Width/FontSize 这类依赖属性需要打开
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        return animation;
    }
}
