using System;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using t_tracker_app.core;

namespace t_tracker_ui.Views;

public sealed partial class AboutPage : Page
{
    public string AppVersion { get; }
    public string DbPath     { get; }
    public string ApiBase    { get; } = "http://localhost:5000";
    public string ConfigPath { get; }
    public string IdleTimeoutSeconds { get; }
    public string ExcludedApps { get; }
    public AppConfig Config { get; }
    
    public AboutPage()
    {
        InitializeComponent();
        Config = AppConfig.Load();
        
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var fileVer = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        var asmVer  = asm.GetName().Version?.ToString();

        AppVersion = infoVer ?? fileVer ?? asmVer ?? "0.0.0";

        DbPath = LogDb.FilePath ?? "(unknown)";

        ApiBase = Config.ApiBaseUrl;
        ConfigPath = Config.FilePath;
        IdleTimeoutSeconds = (Config.IdleTimeoutSeconds/60).ToString() + " minutes";
        ExcludedApps = string.Join(", ", Config.ExcludedApps);

    }
}