using Microsoft.Data.Sqlite;
using System;

namespace t_tracker_app;

public class ScreenLogger : IDisposable
{
    private readonly SqliteConnection _cn;
    private bool _disposed;

    public ScreenLogger() => _cn = LogDb.Open();

    public void Log(string windowTitle, string exeName)
    {
        if (_disposed) return; 
        using var cmd = _cn.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO focus_log (ts, title, exe)
                          VALUES ($ts, $title, $exe);
                          """;
        cmd.Parameters.AddWithValue("$ts",   DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$title",windowTitle);
        cmd.Parameters.AddWithValue("$exe",  exeName);
        cmd.ExecuteNonQuery();
    }

    public void Stop()
    {
        if (_disposed) return; 
        Log("Stopped", "Stopped");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _cn.Dispose();
        _disposed = true;
    }
}