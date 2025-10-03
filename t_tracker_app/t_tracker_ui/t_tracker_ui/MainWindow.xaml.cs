using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.Graphics;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using t_tracker_ui.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace t_tracker_ui;

public sealed partial class MainWindow : Window
{
    private AppWindow _appWindow;
    const int MinW = 640, MinH = 320;

    public MainWindow()
    {
        InitializeComponent();
        ContentFrame.NavigationFailed += async (s, e) =>
        {
            e.Handled = true;
            var dlg = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Navigation failed",
                Content = e.Exception.ToString(),
                CloseButtonText = "OK",
                XamlRoot = (Content as FrameworkElement)?.XamlRoot
            };
            await dlg.ShowAsync();
        };

        WireHoverScaleToNavItems();
        this.AppWindow.Resize(new SizeInt32(800, 500));


        if (MicaController.IsSupported())
        {
            MicaBackdrop micaBackdrop = new MicaBackdrop();
            micaBackdrop.Kind = MicaKind.BaseAlt;
            SystemBackdrop = micaBackdrop;
        }
        else
        {
            SystemBackdrop = null;
            ((Panel)Content).Background = new SolidColorBrush(Color.FromArgb(100, 101, 146, 135));
        }

        _appWindow = AppWindow;
        var tb = _appWindow.TitleBar;
        tb.ExtendsContentIntoTitleBar = true;

        tb.BackgroundColor = Colors.Transparent;
        tb.InactiveBackgroundColor = Colors.Transparent;
        tb.ButtonBackgroundColor = Colors.Transparent;
        tb.ButtonInactiveBackgroundColor = Colors.Transparent;
        ApplyTitleBarTheme();

        SetTitleBar(AppTitleBar);

        UpdateTitleBarLayout();

        _appWindow.Changed += OnAppWindowChanged;
        Activated += OnFirstActivated;
        SizeChanged += (_, __) => ClampWindowSize();

        if (Content is FrameworkElement root)
            root.ActualThemeChanged += (_, __) => ApplyTitleBarTheme();
    }


    void ClampWindowSize()
    {
        var sz = AppWindow.Size;
        int w = Math.Max(sz.Width, MinW);
        int h = Math.Max(sz.Height, MinH);
        if (w != sz.Width || h != sz.Height)
            AppWindow.Resize(new Windows.Graphics.SizeInt32(w, h));
    }
    #region TitleBar
    private void UpdateTitleBarLayout()
    {
        var tb = _appWindow.TitleBar;
        AppTitleBar.Height = tb.Height;
        LeftPaddingCol.Width = new GridLength(tb.LeftInset);
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
    #endregion

    #region Event Handlers
    private void NavView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var first = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .First(i => (string)i.Tag == "DashboardPage");

        NavView.SelectedItem = first;
        if (ContentFrame.CurrentSourcePageType != typeof(DashboardPage))
            ContentFrame.Navigate(typeof(DashboardPage));
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
                case "ChartsPage":
                    if (ContentFrame.CurrentSourcePageType != typeof(ChartsPage))
                        ContentFrame.Navigate(typeof(ChartsPage));
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

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(UpdateTitleBarLayout);
    }
    #endregion



    #region Button Grow On Hover

    void WireHoverScaleToNavItems()
    {
        foreach (var mi in NavView.MenuItems)
            if (mi is NavigationViewItem nvi) HookHover(nvi);

        if (NavView.SettingsItem is NavigationViewItem settings)
            HookHover(settings);
    }
    void HookHover(NavigationViewItem item)
    {
        item.Loaded += (_, __) => EnsureVisualSetup(item);
        item.SizeChanged += (_, __) => EnsureVisualSetup(item);

        item.PointerEntered += (_, __) => AnimateScale(item, 1.08f, 140);
        item.PointerExited += (_, __) => AnimateScale(item, 1.00f, 140);

        item.PointerPressed += (_, __) => AnimateScale(item, 0.98f, 80);
        item.PointerReleased += (_, __) => AnimateScale(item, 1.08f, 120);
    }
    void EnsureVisualSetup(FrameworkElement fe)
    {
        var v = ElementCompositionPreview.GetElementVisual(fe);
        v.CenterPoint = new Vector3(0, (float)fe.ActualHeight / 2f, 0);
        if (v.Scale == default) v.Scale = new Vector3(1, 1, 1);
    }

    void AnimateScale(UIElement el, float target, int ms)
    {
        var v = ElementCompositionPreview.GetElementVisual(el);
        var c = v.Compositor;

        var anim = c.CreateVector3KeyFrameAnimation();
        anim.Duration = TimeSpan.FromMilliseconds(ms);
        anim.InsertKeyFrame(1f, new Vector3(target, target, 1f));
        v.StartAnimation(nameof(v.Scale), anim);
    }
    #endregion

}