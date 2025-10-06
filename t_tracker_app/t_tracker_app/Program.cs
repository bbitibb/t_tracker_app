using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using t_tracker_app;
using t_tracker_app.core;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var appConfig = AppConfig.Load();
builder.Services.AddSingleton(appConfig);

builder.Services.AddSingleton<ScreenLogger>();
builder.Services.AddSingleton<ScreenStatistics>();
builder.Services.AddSingleton<WindowInfoFetcher>();
builder.Services.AddSingleton<DomainLogger>();
builder.Services.AddHostedService<FocusTrackerService>();
builder.Services.AddHostedService<DomainProxyService>();
builder.Services.AddSingleton<DomainStatistics>();

var app = builder.Build();

Console.WriteLine("DB PATH = " + Path.GetFullPath(LogDb.Open().DataSource));
Console.WriteLine($"Config loaded from: {appConfig.FilePath}");
Console.WriteLine($"Idle Timeout: {appConfig.IdleTimeoutSeconds}s");
Console.WriteLine($"Excluded Apps: {string.Join(", ", appConfig.ExcludedApps)}");


app.MapGet("/", () => "t_tracker API running. Use /live or /top");

// GET /live  { "title": "...", "exe": "..." }
app.MapGet("/live", (WindowInfoFetcher fetcher) =>
{
    var (title, exe) = fetcher.GetActiveWindowInfo();
    return Results.Json(new { title, exe });
});

// GET /top?date=2025-07-30&n=10
app.MapGet("/top", (string? date, int? n, ScreenStatistics stats) =>
{
    var day = date is null
        ? DateOnly.FromDateTime(DateTime.Now)
        : DateOnly.Parse(date);

    var rows = stats.LoadDay(day);
    var top = stats.TopN(rows, n ?? 10);
    return Results.Json(top);
});

// GET /health  -> 200 OK JSON if DB opens and todays rows can be read
app.MapGet("/health", (ScreenStatistics stats) =>
{
    try
    {
        var day = DateOnly.FromDateTime(DateTime.Now);
        var rows = stats.LoadDay(day);
        var json = new
        {
            status = "ok",
            dbPath = LogDb.FilePath,
            rowsToday = rows.Count,
            serverTimeUtc = DateTime.UtcNow.ToString("o")
        };
        return Results.Json(json);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Health check failed",
            detail: ex.ToString(),
            statusCode: 500);
    }
});

// GET /export.csv?date=YYYY-MM-DD  -> streamed CSV (download)
app.MapGet("/export.csv", (HttpContext ctx, ScreenStatistics stats, string? date) =>
{
    var day = ParseOrToday(date);
    var rows = stats.LoadDay(day);

    var sb = new StringBuilder();
    sb.AppendLine("start_local,end_local,duration_seconds,duration_hms,exe,title");

    foreach (var e in rows)
    {
        var start = e.Timestamp;
        var end = e.Timestamp.AddSeconds(e.Duration);

        sb.Append(start.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.Append(',');
        sb.Append(end.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.Append(',');
        sb.Append(((int)Math.Round(e.Duration)).ToString(CultureInfo.InvariantCulture));
        sb.Append(',');
        sb.Append(TimeSpan.FromSeconds(e.Duration).ToString(@"hh\:mm\:ss"));
        sb.Append(',');
        sb.Append(CsvEscape(e.Exe));
        sb.Append(',');
        sb.AppendLine(CsvEscape(e.Title));
    }

    var csv = sb.ToString();
    var filename = $"t_tracker_{day:yyyy-MM-dd}.csv";
    ctx.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{filename}\"";
    return Results.Text(csv, "text/csv");
});

app.MapGet("/domains/top", (string? date, int? n) =>
{
    var day = string.IsNullOrWhiteSpace(date) ? DateOnly.FromDateTime(DateTime.Now) : DateOnly.Parse(date);
    var dayStartLocal = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
    var dayEndLocal   = dayStartLocal.AddDays(1);
    var startUtc = dayStartLocal.ToUniversalTime().ToString("o");
    var endUtc   = dayEndLocal.ToUniversalTime().ToString("o");

    using var cn = LogDb.OpenReadOnly();
    using var cmd = cn.CreateCommand();
    cmd.CommandText = """
                        SELECT domain, COUNT(*) AS hits
                        FROM domain_log
                        WHERE ts >= $s AND ts < $e
                        GROUP BY domain
                        ORDER BY hits DESC
                        LIMIT $n;
                      """;
    cmd.Parameters.AddWithValue("$s", startUtc);
    cmd.Parameters.AddWithValue("$e", endUtc);
    cmd.Parameters.AddWithValue("$n", n ?? 10);

    var list = new System.Collections.Generic.List<object>();
    using var r = cmd.ExecuteReader();
    while (r.Read()) list.Add(new { domain = r.GetString(0), hits = r.GetInt64(1) });
    return Results.Json(list);
});
app.MapGet("/domains/top_time", (string? date, int? n, DomainStatistics dom) =>
{
    var day = string.IsNullOrWhiteSpace(date)
        ? DateOnly.FromDateTime(DateTime.Now)
        : DateOnly.Parse(date);

    var (_, top) = dom.LoadDay(day, n ?? 10);
    return Results.Json(top);
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var stats = scope.ServiceProvider.GetRequiredService<ScreenStatistics>();

    var rows = stats.LoadDay(DateOnly.FromDateTime(DateTime.Now));
    var top = stats.TopN(rows, 10);

    Console.WriteLine("\nTop apps today:");
    foreach (var (exe, secs) in top)
        Console.WriteLine($"{exe,-20} {TimeSpan.FromSeconds(secs):hh\\:mm\\:ss}");
});

#region Helpers

static DateOnly ParseOrToday(string? s)
{
    if (!string.IsNullOrWhiteSpace(s) &&
        DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        return d;
    return DateOnly.FromDateTime(DateTime.Now);
}

static string CsvEscape(string? s)
{
    if (string.IsNullOrEmpty(s)) return "\"\"";
    return "\"" + s.Replace("\"", "\"\"") + "\"";
}

#endregion


app.Run("http://localhost:5000");