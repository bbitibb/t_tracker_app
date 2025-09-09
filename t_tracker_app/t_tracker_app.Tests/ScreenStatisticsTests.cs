using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using t_tracker_app;
using Xunit;

namespace t_tracker_app.Tests;

public class ScreenStatisticsTests
{
    [Fact]
    public void CalculateDurations_ComputesExpectedDurations()
    {
        var day = new DateOnly(2023, 1, 1);
        var rows = new List<ScreenStatistics.LogEntry>
        {
            new(day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Local), "t1", "A", 0),
            new(day.ToDateTime(new TimeOnly(8, 5), DateTimeKind.Local), "t2", "B", 0),
            new(day.ToDateTime(new TimeOnly(8, 10), DateTimeKind.Local), "t3", "Stopped", 0),
            new(day.ToDateTime(new TimeOnly(8, 15), DateTimeKind.Local), "t4", "C", 0),
            new(day.ToDateTime(new TimeOnly(8, 20), DateTimeKind.Local), "t5", "Stopped", 0),
        };

        var stats = new ScreenStatistics();
        var result = stats.CalculateDurations(rows);

        Assert.Equal(3, result.Count);
        Assert.Equal(300, result[0].Duration);
        Assert.Equal("A", result[0].Exe);
        Assert.Equal(300, result[1].Duration);
        Assert.Equal("B", result[1].Exe);
        Assert.Equal(300, result[2].Duration);
    }
}