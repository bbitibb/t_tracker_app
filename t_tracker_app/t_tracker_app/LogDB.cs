using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace t_tracker_app;

internal static class LogDb
{
    public static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "t_trackerLogs", "t_tracker.db");

    // open or create DB, ensure table + index exist
    public static SqliteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        var cn = new SqliteConnection($"Data Source={FilePath};Mode=ReadWriteCreate");
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
}