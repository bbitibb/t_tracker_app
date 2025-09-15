using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using t_tracker_ui.State;
using Microsoft.Extensions.DependencyInjection;
using t_tracker_app.core;
using Microsoft.Extensions.Hosting;
using t_tracker_ui.Services;
using t_tracker_ui.ViewModels;
using t_tracker_ui.Views;

namespace t_tracker_ui;
public sealed partial class App : Application
{
    public static UiState State { get; } = new UiState();
    
    public IHost Host { get; }
    
    public App()
    {
        InitializeComponent();
        
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var appConfig = AppConfig.Load();
                services.AddSingleton(appConfig);

                services.AddTransient<MainWindow>();
                services.AddTransient<AboutPage>();
                
                services.AddSingleton<StatsReader>();
                services.AddTransient<DashboardViewModel>();
            })
            .Build();
        
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