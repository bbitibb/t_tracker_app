using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace t_tracker_app;

/// <summary>
/// Background task that polls the active window every 500 ms
/// and writes a log row whenever the window *or* calendar day changes.
/// </summary>
public sealed class FocusTrackerService : BackgroundService
{
    private readonly WindowInfoFetcher _fetcher;
    private readonly ScreenLogger      _logger;
    private readonly ILogger<FocusTrackerService> _log;
    private DateOnly _lastLoggedDay;

    public FocusTrackerService(
        WindowInfoFetcher fetcher,
        ScreenLogger logger,
        ILogger<FocusTrackerService> log)
    {
        _fetcher      = fetcher;
        _logger       = logger;
        _log          = log;
        _lastLoggedDay = DateOnly.FromDateTime(DateTime.Now);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        (string prevTitle, string prevExe) = ("", "");
        _log.LogInformation("Focus-tracker loop started");

        while (!ct.IsCancellationRequested)
        {
            var (title, exe) = _fetcher.GetActiveWindowInfo();
            var today        = DateOnly.FromDateTime(DateTime.Now);

            if (title != prevTitle || exe != prevExe || today != _lastLoggedDay)
            {
                _logger.Log(title, exe);
                prevTitle      = title;
                prevExe        = exe;
                _lastLoggedDay = today;
            }

            await Task.Delay(500, ct);   // poll every 0.5 s
        }
    }

    public override Task StopAsync(CancellationToken ct)
    {
        _log.LogInformation("Focus-tracker stopping - writing final marker");
        _logger.Stop();                 // insert "Stopped"
        return base.StopAsync(ct);
    }
}