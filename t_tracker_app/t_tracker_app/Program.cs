using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

using t_tracker_app;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ScreenLogger>();
builder.Services.AddSingleton<ScreenStatistics>();
builder.Services.AddSingleton<WindowInfoFetcher>(); 

var app = builder.Build();

// ---- REST endpoints ----

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
    var day  = date is null
        ? DateOnly.FromDateTime(DateTime.Now)
        : DateOnly.Parse(date);

    var rows = stats.LoadDay(day);
    var top  = stats.TopN(rows, n ?? 10);
    return Results.Json(top);
});

// graceful CTRL+C handling
Console.CancelKeyPress += (_, e) =>
{
    app.Services.GetRequiredService<ScreenLogger>().Stop();
    e.Cancel = false;
};

app.Lifetime.ApplicationStopping.Register(() =>
{
    // ensure logger connection is disposed
    app.Services.GetRequiredService<ScreenLogger>().Dispose();
});

app.Run("http://localhost:5000");