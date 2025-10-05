using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using t_tracker_app.core;

namespace t_tracker_app;

public sealed class DomainLogger : IDisposable
{
    private readonly SqliteConnection _cn;
    private readonly BlockingCollection<(DateTime tsUtc, string domain, string? url, string source)> _q;
    private readonly CancellationTokenSource _cts;
    private readonly Task _worker;
    private bool _disposed;

    public DomainLogger()
    {
        _cn = LogDb.Open();
        using var prag = _cn.CreateCommand();
        prag.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
        prag.ExecuteNonQuery();

        _q = new(new ConcurrentQueue<(DateTime, string, string?, string)>());
        _cts = new();
        _worker = Task.Run(() => WriterLoop(_cts.Token));
    }

    public void LogDomain(string domain, string? url)
    {
        if (_disposed) return;
        if (string.IsNullOrWhiteSpace(domain)) return;
        _q.Add((DateTime.UtcNow, domain, url, "proxy"));
    }

    private void WriterLoop(CancellationToken ct)
    {
        using var cmd = _cn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO domain_log (ts, domain, url, source)
            VALUES ($ts, $domain, $url, $source);
        """;
        var pTs = cmd.CreateParameter(); pTs.ParameterName = "$ts";     cmd.Parameters.Add(pTs);
        var pDo = cmd.CreateParameter(); pDo.ParameterName = "$domain"; cmd.Parameters.Add(pDo);
        var pUrl= cmd.CreateParameter(); pUrl.ParameterName = "$url";   cmd.Parameters.Add(pUrl);
        var pSo = cmd.CreateParameter(); pSo.ParameterName = "$source"; cmd.Parameters.Add(pSo);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!_q.TryTake(out var first, Timeout.Infinite, ct)) continue;
                using var tx = _cn.BeginTransaction();
                cmd.Transaction = tx;

                void ins((DateTime tsUtc, string domain, string? url, string source) it)
                {
                    pTs.Value = it.tsUtc.ToString("o");
                    pDo.Value = it.domain;
                    pUrl.Value = it.url ?? "";
                    pSo.Value = it.source;
                    cmd.ExecuteNonQuery();
                }

                ins(first);
                int drained = 0;
                while (drained < 200 && _q.TryTake(out var more))
                { ins(more); drained++; }

                tx.Commit();
                cmd.Transaction = null;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Console.Error.WriteLine($"[DomainLogger] {ex}"); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _q.CompleteAdding();
        try { _worker.Wait(TimeSpan.FromSeconds(5)); } catch { _cts.Cancel(); }
        _cn.Dispose(); _q.Dispose(); _cts.Dispose();
    }
}
