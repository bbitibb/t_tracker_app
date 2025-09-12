using System;
using System.IO;
using System.Linq;
using t_tracker_app.core;
using t_tracker_app;
using Xunit;

public class ScreenStatistics_LoadDay_Tests
{
    private static string UseTempDb()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "t_tracker_stats_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "focus.db");
        Environment.SetEnvironmentVariable("T_TRACKER_DB_PATH", dbPath);
        return tempDir;
    }

    private static void CleanupTempDb(string tempDir)
    {
        Environment.SetEnvironmentVariable("T_TRACKER_DB_PATH", null); 
        try { Directory.Delete(tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void LoadDay_CarriesOver_App_At_Midnight_When_No_Midnight_Row()
    {
        var tempDir = UseTempDb();
        try
        {
            var logger = new ScreenLogger();

            var tzNow = DateTime.Now;
            var yesterday = DateOnly.FromDateTime(tzNow.AddDays(-1));
            var today     = DateOnly.FromDateTime(tzNow);

            var y2350 = yesterday.ToDateTime(new TimeOnly(23, 50), DateTimeKind.Local);
            var t0010 = today    .ToDateTime(new TimeOnly( 0, 10), DateTimeKind.Local);

            using (var cn = LogDb.Open())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO focus_log (ts,title,exe) VALUES ($ts,$t,$e);";

                cmd.Parameters.AddWithValue("$ts", y2350.ToUniversalTime().ToString("o"));
                cmd.Parameters.AddWithValue("$t",  "TitleX");
                cmd.Parameters.AddWithValue("$e",  "AppX");
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue("$ts", t0010.ToUniversalTime().ToString("o"));
                cmd.Parameters.AddWithValue("$t",  "TitleY");
                cmd.Parameters.AddWithValue("$e",  "AppY");
                cmd.ExecuteNonQuery();
            }

            var stats = new ScreenStatistics();
            var rowsToday = stats.LoadDay(today);

            var totalX = rowsToday.Where(e => e.Exe == "AppX").Sum(e => e.Duration);
            Assert.InRange(totalX, 9*60, 11*60);
        }
        finally
        {
            CleanupTempDb(tempDir);
        }
    }

    [Fact]
    public void LoadDay_Today_Closes_Trailing_Segment_At_Now_When_No_Next_Row()
    {
        var tempDir = UseTempDb();
        try
        {
            var logger = new ScreenLogger();

            var today = DateOnly.FromDateTime(DateTime.Now);
            var nowBefore = DateTime.Now;

            var t0900 = today.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local);

            using (var cn = LogDb.Open())
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO focus_log (ts,title,exe) VALUES ($ts,$t,$e);";
                cmd.Parameters.AddWithValue("$ts", t0900.ToUniversalTime().ToString("o"));
                cmd.Parameters.AddWithValue("$t",  "Any");
                cmd.Parameters.AddWithValue("$e",  "AppZ");
                cmd.ExecuteNonQuery();
            }

            var stats = new ScreenStatistics();
            var rowsToday = stats.LoadDay(today);

            var nowAfter = DateTime.Now;

            var totalZ = rowsToday.Where(e => e.Exe == "AppZ").Sum(e => e.Duration);

            var expectedMin = (nowBefore - t0900).TotalSeconds - 5;
            var expectedMax = (nowAfter  - t0900).TotalSeconds + 5;

            Assert.InRange(totalZ, expectedMin, expectedMax);
        }
        finally
        {
            CleanupTempDb(tempDir);
        }
    }
}
