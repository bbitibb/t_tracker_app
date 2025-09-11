using System;
using System.Collections.Generic;
using System.Linq;
using t_tracker_app.core;
using t_tracker_app;

namespace t_tracker_ui.Services;

public sealed class StatsReader
{
    private readonly ScreenStatistics _stats = new();

    public (IList<ScreenStatistics.LogEntry> rows, IEnumerable<ScreenStatistics.UsageRow> top)
        LoadDay(DateOnly day, int topN = 10)
    {
        var rows = _stats.LoadDay(day);
        var top  = _stats.TopN(rows, topN);
        return (rows, top);
    }
}