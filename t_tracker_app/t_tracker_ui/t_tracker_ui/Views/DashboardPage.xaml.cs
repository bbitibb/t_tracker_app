using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using t_tracker_ui.Services;

namespace t_tracker_ui.Views;

public sealed partial class DashboardPage : Page
{
    public ObservableCollection<UsageRowVm> Top { get; } = new();
    public int Limit { get; set; } = 10;

    private readonly StatsReader _reader = new();

    public DashboardPage()
    {
        InitializeComponent();
    }

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

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
}

public sealed class UsageRowVm
{
    public int Rank { get; set; }
    public string Exe { get; set; } = "";
    public double Seconds { get; set; }
}