using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using t_tracker_app.core;

namespace t_tracker_app;

public class ScreenLogger : IDisposable
{
    private readonly SqliteConnection _cn;
    private readonly BlockingCollection<(DateTime tsUtc, string title, string exe)> _queue;
    private readonly CancellationTokenSource _cts;
    private readonly Task _worker;
    private bool _disposed;

    public ScreenLogger()
    {
        _cn = LogDb.Open();
        using (var prag = _cn.CreateCommand())
        {
            prag.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
            prag.ExecuteNonQuery();
        }
        _queue = new BlockingCollection<(DateTime, string, string)>(new ConcurrentQueue<(DateTime, string, string)>());
        _cts = new CancellationTokenSource();
        _worker = Task.Run(() => WriterLoop(_cts.Token));
    }


    public void Log(string windowTitle, string exeName)
    {
        if (_disposed) return;
        _queue.Add((DateTime.UtcNow, windowTitle, exeName));
    }

    public void Stop()
    {
        if (_disposed) return;
        Log("Stopped", "Stopped");
    }

    private void WriterLoop(CancellationToken ct)
    {
        using var cmd = _cn.CreateCommand();
        cmd.CommandText = """
                              INSERT INTO focus_log (ts, title, exe)
                              VALUES ($ts, $title, $exe);
                          """;
        var pTs = cmd.CreateParameter(); pTs.ParameterName = "$ts"; cmd.Parameters.Add(pTs);
        var pTitle = cmd.CreateParameter(); pTitle.ParameterName = "$title"; cmd.Parameters.Add(pTitle);
        var pExe = cmd.CreateParameter(); pExe.ParameterName = "$exe"; cmd.Parameters.Add(pExe);

        try
        {
            while (!_queue.IsCompleted && !ct.IsCancellationRequested)
            {
                if (!_queue.TryTake(out var first, Timeout.Infinite, ct)) continue;

                using var tx = _cn.BeginTransaction();
                cmd.Transaction = tx;

                pTs.Value = first.tsUtc.ToString("o");
                pTitle.Value = first.title ?? string.Empty;
                pExe.Value = first.exe ?? string.Empty;
                cmd.ExecuteNonQuery();

                int drained = 0;
                while (drained < 200 && _queue.TryTake(out var item))
                {
                    pTs.Value = item.tsUtc.ToString("o");
                    pTitle.Value = item.title ?? string.Empty;
                    pExe.Value = item.exe ?? string.Empty;
                    cmd.ExecuteNonQuery();
                    drained++;
                }

                tx.Commit();
                cmd.Transaction = null;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ScreenLogger] WriterLoop failed: {ex}");
        }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _queue.CompleteAdding();

        try
        {
            _worker.Wait(TimeSpan.FromSeconds(10));
        }
        catch (AggregateException ae)
        {
        }
        finally
        {
            _cts.Cancel();
            try { _worker.Wait(TimeSpan.FromSeconds(1)); } catch { }
        }

        _cn.Dispose();
        _queue.Dispose();
        _cts.Dispose();
    }
}