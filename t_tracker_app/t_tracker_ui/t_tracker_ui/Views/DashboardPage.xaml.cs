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
    public int Limit { get; set; } = 10;
    public DashboardViewModel ViewModel { get; } = new();
    private readonly StatsReader _reader = new();

    public DashboardPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += async (_, __) => await ViewModel.RefreshAsync();
        DataContext = ViewModel;
        StatsDate.SelectedDate = new DateTimeOffset(DateTime.Now);
    }
    private async void StatsDate_DateChanged(DatePicker sender, DatePickerValueChangedEventArgs args)
        => await ViewModel.RefreshAsync();

    private async void Refresh_Click(object sender, RoutedEventArgs e)
        => await ViewModel.RefreshAsync();
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Top.Clear();
        var day = DateOnly.FromDateTime(DateTime.Now);
        var (_, top) = _reader.LoadDay(day, Limit);

        int rank = 1;
        foreach (var row in top)
        {
            Top.Add(new UsageRowVm
            {
                Rank = rank++,
                Exe = row.exe,
                Seconds = row.secs
            });
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