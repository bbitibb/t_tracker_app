using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace t_tracker_app;

public class ScreenStatistics
{
    public record LogEntry(DateTime Timestamp, string Title, string Exe, double Duration);
    public record UsageRow(string exe, double secs);
    
    /// <summary>Load all rows for the given local day.</summary>
    public IList<LogEntry> LoadDay(DateOnly day)
    {
        var startUtc = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var endUtc   = startUtc.AddDays(1);

        var list = new List<(DateTime ts, string title, string exe)>();
        using var cn  = LogDb.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT ts, title, exe
            FROM   focus_log
            WHERE  ts >= $start AND ts < $end
            ORDER BY ts;
            """;
        cmd.Parameters.AddWithValue("$start", startUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$end",   endUtc  .ToString("o"));

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add((
                DateTime.Parse(r.GetString(0), null, DateTimeStyles.RoundtripKind).ToLocalTime(),
                r.GetString(1),
                r.GetString(2)
            ));
        }

        var outList = new List<LogEntry>();
        for (int i = 0; i < list.Count - 1; i++)
        {
            var cur = list[i];
            var next= list[i + 1];
            if (cur.exe == "Stopped") continue;
            double dur = (next.ts - cur.ts).TotalSeconds;
            outList.Add(new LogEntry(cur.ts, cur.title, cur.exe, dur));
        }
        return outList;
    }
    public IList<LogEntry> CalculateDurations(IList<LogEntry> rows)
    {
        var list = new List<LogEntry>();
        for (int i = 0; i < rows.Count - 1; i++)
        {
            var curr = rows[i];
            var next = rows[i + 1];

            if (curr.Exe == "Stopped") continue;    // skip marker

            double secs = (next.Timestamp - curr.Timestamp).TotalSeconds;
            list.Add(curr with { Duration = secs });  // C# 'with' makes a copy
        }
        return list;
    }
    public IEnumerable<UsageRow> TopN(IList<LogEntry> rows, int n = 10) =>
        rows.GroupBy(r => r.Exe)
            .Select(g => new UsageRow(g.Key, g.Sum(r => r.Duration)))
            .OrderByDescending(r => r.secs)
            .Take(n);
}
