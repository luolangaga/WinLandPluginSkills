using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinIsland.Core;

namespace MyPlugin;

/// <summary>
/// 「超级展开」聚光卡的内容：点击岛体后，宿主会让这张卡片从岛体位置带倾角飞入、居中放大，
/// 点卡片外区域或按 Esc 收起（都有反向动画）。
///
/// 三条要点：
///   1. 必须是**独立于岛视图**的另一棵可视树 —— 每个窗口一棵树，不能把 MyPluginView 传进来；
///   2. 不需要实现 IMorphView：飞入飞回、遮罩、圆角、层级全由宿主负责，这里只管最终形态；
///   3. 定时器跟着 Loaded / Unloaded 起停（宿主收起卡片时会把内容从可视树上卸下），
///      网络请求之类的收尾放 OnHostClosed。
/// </summary>
public sealed class MyPluginSpotlightView : UserControl
{
    private readonly DispatcherQueueTimer _timer;
    private readonly TextBlock _clock;

    public MyPluginSpotlightView(PluginManifest manifest)
    {
        var title = new TextBlock
        {
            Text = manifest.Name,
            FontSize = 26,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
        };

        var description = new TextBlock
        {
            Text = manifest.Description ?? "这里放大卡片里要展示的详细内容：大图表、长列表、更多字段都行。",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 255, 255, 255)),
        };

        _clock = new TextBlock
        {
            FontSize = 13,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(130, 255, 255, 255)),
        };

        var root = new StackPanel
        {
            Spacing = 16,
            Padding = new Thickness(32, 28, 32, 28),
            VerticalAlignment = VerticalAlignment.Center,
        };
        root.Children.Add(title);
        root.Children.Add(description);
        root.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 255, 255)),
        });
        root.Children.Add(_clock);

        Content = root;

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => UpdateClock();

        Loaded += (_, _) =>
        {
            UpdateClock();
            _timer.Start();
        };
        Unloaded += (_, _) => _timer.Stop();
    }

    /// <summary>宿主收起卡片时回调：停表、取消网络请求之类的收尾都放这里。</summary>
    public void OnHostClosed() => _timer.Stop();

    private void UpdateClock() => _clock.Text = $"卡片打开中 · 当前时间 {DateTime.Now:HH:mm:ss}";
}
