using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using t_tracker_app.core;
using t_tracker_ui.Services;
using t_tracker_app;

namespace t_tracker_ui.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly StatsReader _reader = new();

    [ObservableProperty] private string dbPath = LogDb.FilePath;
    [ObservableProperty] private List<ScreenStatistics.UsageRow> top = new();

    public DashboardViewModel() { } 
    public async Task LoadAsync(DateOnly? day = null, int n = 10)
    {
        await Task.Run(() =>
        {
            var d = day ?? DateOnly.FromDateTime(DateTime.Now);
            var (_, t) = _reader.LoadDay(d, n);
            Top = t.ToList();
        });
    }
}