using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using t_tracker_app.core;

namespace t_tracker_app.core;

public sealed class DomainStatistics
{
    public record DomainSlice(DateTime Start, DateTime End, string Domain, double Seconds);
    public record UsageRow(string domain, double secs);

    // Treat these as “browsers” (normalized via AppConfig.NormalizeExeName)
    private static readonly HashSet<string> s_browsers = new(StringComparer.OrdinalIgnoreCase)
        { "chrome", "msedge", "brave", "opera", "firefox" };

    public (IList<DomainSlice> slices, IEnumerable<UsageRow> top) LoadDay(DateOnly day, int topN = 10)
    {
        // 1) Get app-focus segments (you already compute durations here)
        var screenStats = new ScreenStatistics();
        var appRows = screenStats.LoadDay(day)
            .Where(r => s_browsers.Contains(AppConfig.NormalizeExeName(r.Exe))
                        && !string.Equals(r.Exe, "Stopped", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(r.Exe, "Idle",    StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(r.Exe, "Excluded",StringComparison.OrdinalIgnoreCase))
            .ToList(); // entries with per-row Duration already computed. :contentReference[oaicite:3]{index=3}

        // Build focused-browser intervals
        var browserSegments = new List<(DateTime Start, DateTime End)>();
        foreach (var r in appRows)
        {
            var start = r.Timestamp;
            var end   = r.Timestamp.AddSeconds(r.Duration);
            if (end > start) browserSegments.Add((start, end));
        }

        // 2) Pull domain events for the day, plus the last event before day start (like you do for focus)
        GetDayBoundsLocal(day, out var startLocal, out var endLocal);
        var domainEvents = LoadDomainEventsWithPrev(startLocal, endLocal);

        // 3) Walk segments, split by domain events, attribute seconds
        var slices = new List<DomainSlice>();
        foreach (var seg in browserSegments)
        {
            // find the last domain <= seg.Start for initial state
            string current = "(unknown)";
            var prev = domainEvents.LastOrDefault(e => e.tsLocal <= seg.Start);
            if (prev.tsLocal != default) current = prev.domain;

            DateTime cursor = seg.Start;

            foreach (var e in domainEvents.Where(e => e.tsLocal > seg.Start && e.tsLocal < seg.End))
            {
                if (e.tsLocal > cursor)
                {
                    var secs = (e.tsLocal - cursor).TotalSeconds;
                    if (secs > 0 && !IsNoise(current))
                        slices.Add(new DomainSlice(cursor, e.tsLocal, current, secs));
                }
                current = e.domain;
                cursor = e.tsLocal;
            }

            if (seg.End > cursor)
            {
                var secs = (seg.End - cursor).TotalSeconds;
                if (secs > 0 && !IsNoise(current))
                    slices.Add(new DomainSlice(cursor, seg.End, current, secs));
            }
        }

        // 4) Roll-up
        var top = slices
            .GroupBy(s => s.Domain)
            .Select(g => new UsageRow(g.Key, g.Sum(x => x.Seconds)))
            .OrderByDescending(x => x.secs)
            .Take(topN);

        return (slices, top);
    }

    private static bool IsNoise(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return true;
        if (domain.Equals("(unknown)", StringComparison.OrdinalIgnoreCase)) return true;
        if (domain.EndsWith(".local", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static void GetDayBoundsLocal(DateOnly day, out DateTime startLocal, out DateTime endLocal)
    {
        startLocal = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        endLocal   = startLocal.AddDays(1);
    }

    private static List<(DateTime tsLocal, string domain)> LoadDomainEventsWithPrev(DateTime startLocal, DateTime endLocal)
    {
        var list = new List<(DateTime tsLocal, string domain)>();
        using var cn = LogDb.OpenReadOnly();

        // Main window
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = """
              SELECT ts, domain
              FROM domain_log
              WHERE ts >= $s AND ts < $e
              ORDER BY ts;
            """;
            cmd.Parameters.AddWithValue("$s", startLocal.ToUniversalTime().ToString("o"));
            cmd.Parameters.AddWithValue("$e", endLocal  .ToUniversalTime().ToString("o"));

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var tsUtc = DateTime.Parse(r.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind);
                list.Add((tsUtc.ToLocalTime(), r.GetString(1)));
            }
        }

        using (var prevCmd = cn.CreateCommand())
        {
            prevCmd.CommandText = """
              SELECT ts, domain
              FROM domain_log
              WHERE ts < $s
              ORDER BY ts DESC
              LIMIT 1;
            """;
            prevCmd.Parameters.AddWithValue("$s", startLocal.ToUniversalTime().ToString("o"));

            using var pr = prevCmd.ExecuteReader();
            if (pr.Read())
            {
                var tsUtc = DateTime.Parse(pr.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind);
                list.Insert(0, (tsUtc.ToLocalTime(), pr.GetString(1)));
            }
        }

        return list;
    }
}