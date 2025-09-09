using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace t_tracker_app;

/// <summary>
/// Background task that polls the active window every 500 ms
/// and writes a log row whenever the window or calendar day changes.
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
        (string stableTitle, string stableExe) = ("", "");
        (string candTitle, string candExe) = ("", "");
        DateTime candSince = DateTime.MinValue;

        const int debounceMs = 600;

        _log.LogInformation("Focus-tracker loop started");

        _lastLoggedDay = DateOnly.FromDateTime(DateTime.Now);

        while (!ct.IsCancellationRequested)
        {
            var (title, exe) = _fetcher.GetActiveWindowInfo();
            var today        = DateOnly.FromDateTime(DateTime.Now);

            var dayChanged = today != _lastLoggedDay;

            if (title != candTitle || exe != candExe || dayChanged)
            {
                candTitle = title;
                candExe   = exe;
                candSince = DateTime.UtcNow;
                if (dayChanged)
                    _lastLoggedDay = today;
            }
            else
            {
                var stableForMs = (DateTime.UtcNow - candSince).TotalMilliseconds;
                if (stableForMs >= debounceMs &&
                    (candTitle != stableTitle || candExe != stableExe))
                {
                    _logger.Log(candTitle, candExe);
                    stableTitle = candTitle;
                    stableExe   = candExe;
                }
            }

            await Task.Delay(500, ct);
        }
    }

    public override Task StopAsync(CancellationToken ct)
    {
        _log.LogInformation("Focus-tracker stopping - writing final marker");
        _logger.Stop();
        return base.StopAsync(ct);
    }
}