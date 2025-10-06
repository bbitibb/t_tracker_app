using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using t_tracker_app.core;
using t_tracker_ui.Services;
using t_tracker_ui.State;
using t_tracker_ui.Util;
using t_tracker_ui.ViewModels;

namespace t_tracker_ui.Views;

public sealed partial class DashboardPage : Page
{
    public ObservableCollection<UsageRowVm> Top { get; } = new();
    public DashboardViewModel ViewModel { get; } = new();
    private readonly StatsReader _reader = new();
    private readonly DispatcherTimer _autoTimer = new();

    private readonly SecondsToHmsConverter _hms = new();
    private UsageRowVm? _tipRow;

    
    public DashboardPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        DataContext = ViewModel;
        
        StatsDate.SelectedDate = App.State.SelectedDate;
        App.State.PropertyChanged += OnAppStateChanged;
        
        _autoTimer.Interval = TimeSpan.FromSeconds(1);
        _autoTimer.Tick += async (_, __) =>
        {
            var selected = DateOnly.FromDateTime(App.State.SelectedDate.DateTime);
            if (selected == DateOnly.FromDateTime(DateTime.Now))
                await ViewModel.RefreshAsync();
        };
        
        StatsDate.DateChanged += async (_, args) =>
        {
            App.State.SelectedDate = args.NewDate;
            await ViewModel.RefreshAsync();
        };

        Loaded += async (_, __) => await ViewModel.RefreshAsync();
    }
    private void TopList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not UsageRowVm row) return;

        var container = TopList.ContainerFromItem(row) as ListViewItem;

        FrameworkElement anchor = container as FrameworkElement ?? TopList;

        _tipRow = row;
        RowTip.Title = row.Exe;
        RowTip.Subtitle = (string)_hms.Convert(row.Seconds, typeof(string), null, "");

        RowTip.Target = anchor;
        RowTip.IsOpen = true;
    }
    private async void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UiState.SelectedDate))
        {
            StatsDate.SelectedDate = App.State.SelectedDate;
            await ViewModel.RefreshAsync();
        }
    }
    private void RowTip_CloseButtonClick(TeachingTip sender, object args)
    {
        _tipRow = null;
    }
    private async void RowTip_ActionButtonClick(TeachingTip sender, object args)
    {
        if (_tipRow is null) return;

        await ExcludeExeAsync(_tipRow.Exe);

        RowTip.IsOpen = false;
        _tipRow = null;
    }
    private async Task ExcludeExeAsync(string exe)
    {
        try
        {
            var config = AppConfig.Load();
            var normalized = AppConfig.NormalizeExeName(exe);

            if (!config.IsExcludedApp(normalized))
            {
                config.ExcludedApps.Add(normalized);
                config.NormalizeExcludedApps();
                config.Save();
            }

            await ViewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exclude failed: {ex}");
        }
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _autoTimer.Start();
        StatsDate.Date = App.State.SelectedDate;
    }
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _autoTimer.Stop();
    }
    private async Task LoadAsync()
    {
        Top.Clear();
        var day = DateOnly.FromDateTime(DateTime.Now);
        var (_, top) = _reader.LoadDay(day, ViewModel.Limit);

        int rank = 1;
        foreach (var row in top)
        {
            if (row.exe != "Excluded")
            {
                Top.Add(new UsageRowVm
                {
                    Rank = rank++,
                    Exe = row.exe,
                    Seconds = row.secs
                });
            }
        }
        await Task.CompletedTask;
    }

}

public sealed class UsageRowVm
{
    public int Rank { get; set; }
    public string Exe { get; set; } = "";
    public double Seconds { get; set; }
}