using System.Linq;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using t_tracker_ui.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;

namespace t_tracker_ui;

public sealed partial class MainWindow : Window
{
    private AppWindow _appWindow;
    public MainWindow()
    {
        InitializeComponent();

        SystemBackdrop = new MicaBackdrop();

        _appWindow = AppWindow;
        var tb = _appWindow.TitleBar;
        tb.ExtendsContentIntoTitleBar = true;

        tb.BackgroundColor = Colors.Transparent;
        tb.InactiveBackgroundColor = Colors.Transparent;
        tb.ButtonBackgroundColor = Colors.Transparent;
        tb.ButtonInactiveBackgroundColor = Colors.Transparent;
        ApplyTitleBarTheme();

        SetTitleBar(AppTitleBar);

        _appWindow.Changed += OnAppWindowChanged;
        UpdateTitleBarLayout();

        Activated += OnFirstActivated;

        if (Content is FrameworkElement root)
            root.ActualThemeChanged += (_, __) => ApplyTitleBarTheme();
    }
    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag as string)
            {
                case "StatisticsPage":
                    if (ContentFrame.CurrentSourcePageType != typeof(DashboardPage))
                        ContentFrame.Navigate(typeof(DashboardPage));
                    break;
                case "AboutPage":
                    if (ContentFrame.CurrentSourcePageType != typeof(AboutPage))
                        ContentFrame.Navigate(typeof(AboutPage));
                    break;
            }
        }
    }
    private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        Activated -= OnFirstActivated;

        foreach (var mi in NavView.MenuItems)
        {
            if (mi is NavigationViewItem nvi && (string)nvi.Tag == "StatisticsPage")
            {
                NavView.SelectedItem = nvi;
                break;
            }
        }

        if (ContentFrame.CurrentSourcePageType != typeof(DashboardPage))
            ContentFrame.Navigate(typeof(DashboardPage));
    }
    private void UpdateTitleBarLayout()
    {
        var tb = _appWindow.TitleBar;
        AppTitleBar.Height = tb.Height;
        LeftPaddingCol.Width  = new GridLength(tb.LeftInset);
        RightPaddingCol.Width = new GridLength(tb.RightInset);
    }
    private void ApplyTitleBarTheme()
    {
        var isDark = (Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark;
        var fg = isDark ? Colors.White : Colors.Black;

        var tb = _appWindow.TitleBar;
        tb.ForegroundColor = fg;
        tb.InactiveForegroundColor = fg;
        tb.ButtonForegroundColor = fg;
        tb.ButtonInactiveForegroundColor = fg;
    }
    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(UpdateTitleBarLayout);
    }
}