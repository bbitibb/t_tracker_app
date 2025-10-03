using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace t_tracker_app.core;

public static class LogDb
{
    public static string FilePath;
    static LogDb()
    {
        FilePath = ResolvePath();
    }

    public static string ResolvePath()
    {
        var overridePath = Environment.GetEnvironmentVariable("T_TRACKER_DB_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(overridePath)!);
            return overridePath;
        }

        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "t_trackerLogs", "t_tracker.db");

        Directory.CreateDirectory(Path.GetDirectoryName(defaultPath)!);
        return defaultPath;
    }

    public static SqliteConnection Open()
    {
        var filePath = ResolvePath();

        var cn = new SqliteConnection($"Data Source={filePath};Mode=ReadWriteCreate");
        cn.Open();

        const string schema = """
                              CREATE TABLE IF NOT EXISTS focus_log (
                                  id      INTEGER PRIMARY KEY AUTOINCREMENT,
                                  ts      TEXT    NOT NULL,       
                                  title   TEXT    NOT NULL,
                                  exe     TEXT    NOT NULL
                              );
                              CREATE INDEX IF NOT EXISTS idx_ts ON focus_log (ts);
                              """;
        using var cmd = cn.CreateCommand();

        cmd.CommandText = schema;
        cmd.ExecuteNonQuery();

        return cn;
    }
    public static SqliteConnection OpenReadOnly()
    {
        var filePath = ResolvePath();
        var cn = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly;Cache=Shared");
        cn.Open();
        return cn;
    }
}