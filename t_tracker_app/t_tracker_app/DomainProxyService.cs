using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using t_tracker_app.core;

namespace t_tracker_app;

public sealed class DomainProxyService : BackgroundService, IDisposable
{
    private readonly AppConfig _config;
    private readonly DomainLogger _log;
    private readonly ILogger<DomainProxyService> _logger;
    private SimpleProxy? _proxy;

    public DomainProxyService(AppConfig config, DomainLogger domainLogger, ILogger<DomainProxyService> logger)
    {
        _config = config;
        _log = domainLogger;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.EnableProxyTracking) { _logger.LogInformation("Proxy tracking disabled."); return Task.CompletedTask; }

        _proxy = new SimpleProxy(_config.ProxyPort);
        _proxy.Start(async (domain, url) =>
        {
            _log.LogDomain(domain, url);
            await Task.CompletedTask;
        });

        _logger.LogInformation("Local proxy started on 127.0.0.1:{port}", _config.ProxyPort);

        if (_config.SetSystemProxy)
        {
            try { SystemProxy.EnableLocalProxy(_config.ProxyPort); _logger.LogInformation("System proxy enabled for current user."); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to enable system proxy"); }
        }

        // Keep running until stop
        stoppingToken.Register(() =>
        {
            if (_config.SetSystemProxy)
            {
                try { SystemProxy.DisableProxy(); } catch { }
            }
            _proxy?.Dispose();
        });

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (_config.SetSystemProxy)
        {
            try { SystemProxy.DisableProxy(); } catch { }
        }
        _proxy?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}