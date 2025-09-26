using Microsoft.Win32;
using System;
using Microsoft.Extensions.Logging; 
using t_tracker_app.core;
using System.Runtime.InteropServices;

namespace t_tracker_app;
public sealed class FocusTrackerService : BackgroundService, IDisposable
{
    private readonly WindowInfoFetcher _fetcher;
    private readonly ScreenLogger _logger;
    private readonly ILogger<FocusTrackerService> _log;
    private readonly AppConfig _config;
    private DateOnly _lastLoggedDay;
    private bool _wasIdle = false;
    private DateTime _lastActivityTime;
    private volatile bool _forceLogNext = false;
    
    private int IdleCutoffSeconds => Math.Max(0, _config.IdleTimeoutSeconds);
    public FocusTrackerService(
        WindowInfoFetcher fetcher,
        ScreenLogger logger,
        ILogger<FocusTrackerService> log,
        AppConfig config)
    {
        _fetcher       = fetcher;
        _logger        = logger;
        _log           = log;
        _config        = config;
        _lastLoggedDay = DateOnly.FromDateTime(DateTime.Now);
        _lastActivityTime = DateTime.UtcNow;
        
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock)
        {
            _log.LogInformation("System locked - writing Stopped marker");
            _logger.Stop();
            _wasIdle = true;
        }
        else if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            _log.LogInformation("System unlocked - will resume logging normally");
            _lastActivityTime = DateTime.UtcNow;
            _forceLogNext = true;
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            _log.LogInformation("System suspend - writing Stopped marker");
            _logger.Stop();
            _wasIdle = true;
        }
        else if (e.Mode == PowerModes.Resume)
        {
            _log.LogInformation("System resume - will resume logging normally");
            _lastActivityTime = DateTime.UtcNow;
            _forceLogNext = true;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        (string prevTitle, string prevExe) = ("", "");
        _log.LogInformation("Focus-tracker loop started");

        while (!ct.IsCancellationRequested)
        {
            var idleSecs = GetIdleSeconds();
            var cutoff   = IdleCutoffSeconds;
            
            var (title, exeRaw) = _fetcher.GetActiveWindowInfo();
            var exe = NormalizeExe(exeRaw);
            var today = DateOnly.FromDateTime(DateTime.Now);
            bool isIdle = cutoff > 0 && idleSecs >= cutoff;
            bool shouldForce = _wasIdle || _forceLogNext;
            
            if (isIdle)
            {
                if (!_wasIdle)
                {
                    _log.LogInformation($"Idle for {cutoff} seconds → logging Idle");
                    _logger.Log("Idle", "Idle");
                    prevTitle = "Idle";
                    prevExe   = "Idle";
                    _wasIdle  = true;
                }
            }
            else if (_config.ExcludedApps.Contains(exe, StringComparer.OrdinalIgnoreCase))
            {
                if (prevExe != "Excluded")
                {
                    _log.LogInformation($"Excluded app '{exe}' detected → logging Excluded");
                    _logger.Log("Excluded", "Excluded");
                    prevTitle = "Excluded";
                    prevExe = "Excluded";
                }
                _lastActivityTime = DateTime.UtcNow;
            }
            else if (shouldForce || title != prevTitle || exe != prevExe || today != _lastLoggedDay || _forceLogNext)
            {
                _log.LogInformation($"Logging: Title='{title}', Exe='{exe}'");
                _logger.Log(title, exe);
                prevTitle      = title;
                prevExe        = exe;
                _lastLoggedDay = today;
                _lastActivityTime = DateTime.UtcNow;
                _forceLogNext  = false;
                _wasIdle  = false;
            }

            await Task.Delay(500, ct);
        }
    }
    public override async Task StopAsync(CancellationToken ct)
    {
        _log.LogInformation("Focus-tracker stopping - writing final marker");
        _logger.Stop();

        await Task.Delay(100, ct);

        await base.StopAsync(ct);
    }

    public override void Dispose()
    {
        SystemEvents.SessionSwitch     -= OnSessionSwitch;
        SystemEvents.PowerModeChanged  -= OnPowerModeChanged;

        base.Dispose();
    }
    
    private static string NormalizeExe(string exeOrProcessName)
        => exeOrProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? exeOrProcessName
            : exeOrProcessName + ".exe";
    
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
    private static double GetIdleSeconds()
    {
        LASTINPUTINFO lii = new() { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref lii)) return 0;
        uint tickCount = GetTickCount();
        uint delta = tickCount - lii.dwTime; // milliseconds
        return delta / 1000.0;
    }
    
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();
}
