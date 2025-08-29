using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

using t_tracker_app;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ScreenLogger>();
builder.Services.AddSingleton<ScreenStatistics>();
builder.Services.AddSingleton<WindowInfoFetcher>(); 

builder.Services.AddHostedService<FocusTrackerService>(); 

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


app.Lifetime.ApplicationStopping.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var stats = scope.ServiceProvider.GetRequiredService<ScreenStatistics>();
    
    var rows = stats.LoadDay(DateOnly.FromDateTime(DateTime.Now));
    var top  = stats.TopN(rows, 10);
    
    Console.WriteLine("\nTop apps today:");
    foreach (var (exe, secs) in top)
        Console.WriteLine($"{exe,-20} {TimeSpan.FromSeconds(secs):hh\\:mm\\:ss}");
});

app.Run("http://localhost:5000");