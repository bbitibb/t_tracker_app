using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using t_tracker_ui.Views;

namespace t_tracker_ui;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        
        InitializeComponent();
        
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
}