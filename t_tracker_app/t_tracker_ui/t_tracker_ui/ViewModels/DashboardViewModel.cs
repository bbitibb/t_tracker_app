using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using t_tracker_app.core;
using t_tracker_ui.Services;
using SS = t_tracker_app.core.ScreenStatistics;
using t_tracker_ui.Views;

namespace t_tracker_ui.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTimeOffset selectedDate = DateTimeOffset.Now.Date;
    [ObservableProperty]
    private int limit = 10;
    
    private readonly StatsReader _stats = new();
    public ObservableCollection<UsageRowVm> Top { get; } = new();

    partial void OnSelectedDateChanged(DateTimeOffset value) => _ = RefreshAsync();
    partial void OnLimitChanged(int value) => _ = RefreshAsync();
    public async Task RefreshAsync()
    {
        var date = DateOnly.FromDateTime(SelectedDate.Date);
        var config = AppConfig.Load();
        var topCount = Limit + config.ExcludedApps.Count + 1;
        var (_, topRaw) = await Task.Run(() => _stats.LoadDay(date, topCount));
        
        var top = topRaw
            .Where(u => !config.ExcludedApps.Contains(u.exe, StringComparer.OrdinalIgnoreCase)
                        && !string.Equals(u.exe, "Idle", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(u.exe, "Stopped", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(u.exe, "Excluded", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(u => u.secs)
            .Take(Limit)
            .Select((u, i) => new UsageRowVm
            {
                Rank = i + 1,
                Exe = u.exe,                   
                Seconds = u.secs
            });

        
        Top.Clear();
        foreach (var item in top) Top.Add(item);
    }
}