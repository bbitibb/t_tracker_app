using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace t_tracker_app
{
    public class ScreenStatistics
    {
        private DateTime _startTime;
        private DateTime _endTime;

        public ScreenStatistics()
        {
            
        }
        
        public ScreenStatistics(DateTime date)
        {
            _startTime = date;
        }
        
        public ScreenStatistics(DateTime start, DateTime end)
        {
            _startTime = start;
            _endTime = end;
        }
        
        public List<LogEntry> ParseEntries(string logFilePath)
        {
            var lines = File.ReadAllLines(logFilePath).Skip(1); // skip header
            var entries = new List<LogEntry>();

            foreach (var line in lines)
            {
                var parts = SplitCsv(line);
                if (parts.Length < 3) continue;
                if (string.IsNullOrWhiteSpace(parts[2])) continue; // exe_name missing

                if (!DateTime.TryParseExact(
                        parts[0],
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var timestamp))
                    continue;

                entries.Add(new LogEntry
                {
                    Timestamp = timestamp,
                    WindowTitle = parts[1],
                    ExeName = parts[2]
                });
            }
            return entries.OrderBy(e => e.Timestamp).ToList();
        }

        public List<LogEntry> CalculateEntryDurations(List<LogEntry> entries)
        {
            var newEntries = new List<LogEntry>();
            for (int i = 0; i < entries.Count - 1; i++)
            {
                var curr = entries[i];
                var next = entries[i + 1];

                double duration = 0;
                if (curr.ExeName != "Stopped" && next.ExeName != "Stopped")
                {
                    duration = (next.Timestamp - curr.Timestamp).TotalSeconds;
                }
                else if (curr.ExeName != "Stopped" && next.ExeName == "Stopped")
                {
                    duration = (next.Timestamp - curr.Timestamp).TotalSeconds;
                }

                if (curr.ExeName != "Stopped")
                {
                    newEntries.Add(new LogEntry
                    {
                        Timestamp = curr.Timestamp,
                        WindowTitle = curr.WindowTitle,
                        ExeName = curr.ExeName,
                        Duration = duration
                    });
                }
            }
            return newEntries;
        }

        public List<(string ExeName, double Duration)> CalculateUsage(List<LogEntry> durationEntries, int topN = 10)
        {
            var entries = CalculateEntryDurations(durationEntries);
            return entries
                .Take(entries.Count - 1)
                .Where(e => e.ExeName != "Stopped")
                .GroupBy(e => e.ExeName)
                .Select(g => (ExeName: g.Key, Duration: g.Sum(e => e.Duration)))
                .Where(x => x.Duration > 0)
                .OrderByDescending(x => x.Duration)
                .Take(topN)
                .ToList();
        }

        public void PrintTopApps(List<(string ExeName, double Duration)> usage)
        {
            foreach (var app in usage)
            {
                Console.WriteLine($"{app.ExeName}: {Fmt(app.Duration)}");
            }
        }

        // Helper to split CSV with basic quoting support
        private string[] SplitCsv(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var cur = "";
            foreach (var c in line)
            {
                if (c == ',' && !inQuotes)
                {
                    result.Add(cur);
                    cur = "";
                }
                else if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else
                {
                    cur += c;
                }
            }
            result.Add(cur);
            return result.ToArray();
        }

        // Format seconds as H:M:S
        private string Fmt(double seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            return $"{(int)t.TotalHours}h {t.Minutes}m {t.Seconds}s";
        }

        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string WindowTitle { get; set; }
            public string ExeName { get; set; }
            public double Duration { get; set; }
        }
    }
}