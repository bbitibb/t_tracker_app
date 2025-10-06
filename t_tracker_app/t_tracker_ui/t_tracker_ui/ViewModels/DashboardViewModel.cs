using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using t_tracker_app.core;
using t_tracker_ui.Services;
using t_tracker_ui.State;
using SS = t_tracker_app.core.ScreenStatistics;
using t_tracker_ui.Views;

namespace t_tracker_ui.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public DateTimeOffset SelectedDate
    {
        get => App.State.SelectedDate;
        set
        {
            if (App.State.SelectedDate != value)
            {
                App.State.SelectedDate = value;
                OnPropertyChanged();
                _ = RefreshAsync();
            }
        }
    }

    public int Limit
    {
        get => App.State.Limit;
        set
        {
            if (App.State.Limit != value)
            {
                App.State.Limit = value;
                OnPropertyChanged();
                _ = RefreshAsync();
            }
        }
    }

    private readonly StatsReader _stats = new();
    public ObservableCollection<UsageRowVm> Top { get; } = new();

    public DashboardViewModel()
    {
        App.State.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UiState.SelectedDate))
                OnPropertyChanged(nameof(SelectedDate));
        };
    }
    
    public async Task RefreshAsync()
    {
        var date = DateOnly.FromDateTime(App.State.SelectedDate.Date);
        var config = AppConfig.Load();
        var topCount = Limit + config.ExcludedApps.Count + 1;
        var (_, topRaw) = await Task.Run(() => _stats.LoadDay(date, topCount));

        var top = topRaw
            .Where(u => !config.IsExcludedApp(u.exe)
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