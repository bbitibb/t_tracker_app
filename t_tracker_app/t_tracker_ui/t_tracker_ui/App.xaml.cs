using Microsoft.UI.Xaml;
namespace t_tracker_ui;
public sealed partial class App : Application
{
    public App(){ InitializeComponent(); }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
        => new MainWindow().Activate();
}