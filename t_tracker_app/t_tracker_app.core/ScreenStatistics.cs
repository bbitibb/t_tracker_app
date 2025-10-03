using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace t_tracker_app.core;

public class ScreenStatistics
{
    public record LogEntry(DateTime Timestamp, string Title, string Exe, double Duration);
    public record UsageRow(string exe, double secs);

    /// <summary>Load all rows for the given local day.</summary>
    public IList<LogEntry> LoadDay(DateOnly day)
    {
        var dayStartLocal = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        var dayEndLocal = dayStartLocal.AddDays(1);
        var startUtc = dayStartLocal.ToUniversalTime();
        var endUtc = dayEndLocal.ToUniversalTime();

        var rows = new List<(DateTime tsLocal, string title, string exe)>();

        using var cn = LogDb.OpenReadOnly();
        using var cmd = cn.CreateCommand();

        cmd.CommandText = """
        SELECT ts, title, exe
        FROM   focus_log
        WHERE  ts >= $start AND ts < $end
        ORDER BY ts;
        """;
        cmd.Parameters.AddWithValue("$start", startUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$end", endUtc.ToString("o"));

        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                var tsUtc = DateTime.Parse(r.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind);
                rows.Add((tsUtc.ToLocalTime(), r.GetString(1), r.GetString(2)));
            }
        }

        using (var prevCmd = cn.CreateCommand())
        {
            prevCmd.CommandText = """
            SELECT ts, title, exe
            FROM   focus_log
            WHERE  ts < $start
            ORDER BY ts DESC
            LIMIT 1;
            """;
            prevCmd.Parameters.AddWithValue("$start", startUtc.ToString("o"));

            using var pr = prevCmd.ExecuteReader();
            if (pr.Read())
            {
                var prevUtc = DateTime.Parse(pr.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind);
                var prevLocal = prevUtc.ToLocalTime();
                var prevTitle = pr.GetString(1);
                var prevExe = pr.GetString(2);

                if (!string.Equals(prevExe, "Stopped", StringComparison.OrdinalIgnoreCase))
                {
                    if (rows.Count == 0 || rows[0].tsLocal > dayStartLocal ||
                        !string.Equals(rows[0].exe, prevExe, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(rows[0].title, prevTitle, StringComparison.Ordinal))
                    {
                        rows.Insert(0, (dayStartLocal, prevTitle, prevExe));
                    }
                }
            }
        }

        var result = new List<LogEntry>();
        var nowLocal = DateTime.Now;
        var isToday = day == DateOnly.FromDateTime(nowLocal);

        for (int i = 0; i < rows.Count; i++)
        {
            var cur = rows[i];

            if (string.Equals(cur.exe, "Stopped", StringComparison.OrdinalIgnoreCase))
                continue;

            DateTime nextLocal;
            if (i + 1 < rows.Count)
                nextLocal = rows[i + 1].tsLocal;
            else
                nextLocal = isToday ? nowLocal : dayEndLocal;

            if (nextLocal <= cur.tsLocal)
                continue;

            var segStart = cur.tsLocal < dayStartLocal ? dayStartLocal : cur.tsLocal;
            var segEnd = nextLocal > dayEndLocal ? dayEndLocal : nextLocal;

            if (segEnd <= segStart)
                continue;

            var durSeconds = (segEnd - segStart).TotalSeconds;
            result.Add(new LogEntry(cur.tsLocal, cur.title, cur.exe, durSeconds));
        }

        return result;
    }
    public IList<LogEntry> CalculateDurations(IList<LogEntry> rows)
    {
        var list = new List<LogEntry>();
        for (int i = 0; i < rows.Count - 1; i++)
        {
            var curr = rows[i];
            var next = rows[i + 1];

            if (curr.Exe == "Stopped") continue;

            double secs = (next.Timestamp - curr.Timestamp).TotalSeconds;
            list.Add(curr with { Duration = secs });
        }
        return list;
    }
    public IEnumerable<UsageRow> TopN(IList<LogEntry> rows, int n = 10) =>
        rows.GroupBy(r => r.Exe)
            .Select(g => new UsageRow(g.Key, g.Sum(r => r.Duration)))
            .OrderByDescending(r => r.secs)
            .Take(n);

    private static IEnumerable<(DateTime Start, DateTime End, string Title, string Exe)> ClampToLocalDay(List<(DateTime tsLocal, string title, string exe)> rows, DateOnly day)
    {
        var dayStart = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        var dayEnd = dayStart.AddDays(1);

        for (int i = 0; i < rows.Count; i++)
        {
            var cur = rows[i];
            var next = (i + 1 < rows.Count) ? rows[i + 1].tsLocal : DateTime.Now;

            if (string.Equals(cur.exe, "Stopped", StringComparison.OrdinalIgnoreCase))
                continue;

            var segStart = cur.tsLocal;
            var segEnd = next;

            var start = segStart < dayStart ? dayStart : segStart;
            var end = segEnd > dayEnd ? dayEnd : segEnd;

            if (end > start)
                yield return (start, end, cur.title, cur.exe);
        }
    }
}
