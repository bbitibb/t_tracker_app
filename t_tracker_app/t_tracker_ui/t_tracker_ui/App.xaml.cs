using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using t_tracker_ui.State;

namespace t_tracker_ui;
public sealed partial class App : Application
{
    public static UiState State { get; } = new UiState();
    public App()
    {
        InitializeComponent();
        UnhandledException += async (s, e) =>
        {
            e.Handled = true;
            var dlg = new ContentDialog
            {
                Title = "Unhandled exception",
                Content = e.Exception.ToString(),
                CloseButtonText = "OK"
            };
            await dlg.ShowAsync();
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            Debug.WriteLine("AppDomain.Unhandled: " + e.ExceptionObject);
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
        => new MainWindow().Activate();
}