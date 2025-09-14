using Microsoft.Win32;
using System;

namespace t_tracker_app;

public sealed class FocusTrackerService : BackgroundService, IDisposable
{
    private readonly WindowInfoFetcher _fetcher;
    private readonly ScreenLogger _logger;
    private readonly ILogger<FocusTrackerService> _log;
    private DateOnly _lastLoggedDay;

    public FocusTrackerService(
        WindowInfoFetcher fetcher,
        ScreenLogger logger,
        ILogger<FocusTrackerService> log)
    {
        _fetcher       = fetcher;
        _logger        = logger;
        _log           = log;
        _lastLoggedDay = DateOnly.FromDateTime(DateTime.Now);

        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock)
        {
            _log.LogInformation("System locked → writing Stopped marker");
            _logger.Stop();
        }
        else if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            _log.LogInformation("System unlocked → will resume logging normally");
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            _log.LogInformation("System suspend → writing Stopped marker");
            _logger.Stop();
        }
        else if (e.Mode == PowerModes.Resume)
        {
            _log.LogInformation("System resume → will resume logging normally");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        (string prevTitle, string prevExe) = ("", "");
        _log.LogInformation("Focus-tracker loop started");

        while (!ct.IsCancellationRequested)
        {
            var (title, exe) = _fetcher.GetActiveWindowInfo();
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (title != prevTitle || exe != prevExe || today != _lastLoggedDay)
            {
                Console.WriteLine("Logged");
                _logger.Log(title, exe);
                prevTitle      = title;
                prevExe        = exe;
                _lastLoggedDay = today;
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
}
