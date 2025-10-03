using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using t_tracker_ui.Services;
using t_tracker_ui.ViewModels;

namespace t_tracker_ui.Views;

public sealed partial class DashboardPage : Page
{
    public ObservableCollection<UsageRowVm> Top { get; } = new();
    public DashboardViewModel ViewModel { get; } = new();
    private readonly StatsReader _reader = new();
    private readonly DispatcherTimer _autoTimer = new();

    public DashboardPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += async (_, __) => await ViewModel.RefreshAsync();
        DataContext = ViewModel;
        StatsDate.SelectedDate = new DateTimeOffset(DateTime.Now);
        _autoTimer.Interval = TimeSpan.FromSeconds(1);
        _autoTimer.Tick += async (_, __) =>
        {
            var selected = DateOnly.FromDateTime(StatsDate.Date.DateTime);
            if (selected == DateOnly.FromDateTime(DateTime.Now))
                await ViewModel.RefreshAsync();
        };
        
    }
    private async void StatsDate_DateChanged(DatePicker sender, DatePickerValueChangedEventArgs args)
        => await ViewModel.RefreshAsync();
    
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //await LoadAsync();
        _autoTimer.Start(); 
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