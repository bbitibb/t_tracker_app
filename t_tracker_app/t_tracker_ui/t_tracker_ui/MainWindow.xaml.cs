using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using t_tracker_ui.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI;
namespace t_tracker_ui;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        
        InitializeComponent();
        
        AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 500));

        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

        if (Content is FrameworkElement root)
            root.ActualThemeChanged += (_, __) => ApplyTitleBarTheme();

        this.Activated += OnFirstActivated;
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
        this.Activated -= OnFirstActivated;

        if (NavView.MenuItems.Count > 0)
            NavView.SelectedItem = NavView.MenuItems[0];

        if (ContentFrame.CurrentSourcePageType != typeof(DashboardPage))
            ContentFrame.Navigate(typeof(DashboardPage));
    }
    private void ApplyTitleBarTheme()
    {
        var appWindow = this.AppWindow;
        if (!AppWindowTitleBar.IsCustomizationSupported())
            return;

        var titleBar = appWindow.TitleBar;

        var isDark = (Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark;

        Color bg = isDark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);
        Color fg = isDark ? Colors.White : Colors.Black;

        titleBar.ExtendsContentIntoTitleBar = false;

        titleBar.BackgroundColor                 = bg;
        titleBar.ForegroundColor                 = fg;
        titleBar.InactiveBackgroundColor         = bg;
        titleBar.InactiveForegroundColor         = fg;

        titleBar.ButtonBackgroundColor           = bg;
        titleBar.ButtonForegroundColor           = fg;
        titleBar.ButtonInactiveBackgroundColor   = bg;
        titleBar.ButtonInactiveForegroundColor   = fg;
    }
}