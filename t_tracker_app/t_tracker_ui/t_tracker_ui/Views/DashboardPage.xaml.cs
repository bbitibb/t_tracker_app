using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using t_tracker_app.core;
using t_tracker_ui.Services;
using t_tracker_ui.State;
using t_tracker_ui.Util;
using t_tracker_ui.ViewModels;
using System.Linq;
using Microsoft.UI.Dispatching;  

namespace t_tracker_ui.Views;

public sealed partial class DashboardPage : Page
{
    public ObservableCollection<UsageRowVm> Top { get; } = new();
    public DashboardViewModel ViewModel { get; } = new();
    private readonly StatsReader _reader = new();
    private readonly DispatcherTimer _autoTimer = new();

    private readonly SecondsToHmsConverter _hms = new();
    private UsageRowVm? _currentRow;

    
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
        _currentRow = e.ClickedItem as UsageRowVm;
        if (_currentRow == null) return;

        NameEditor.Text = AppConfig.Load().GetDisplayNameOrExe(_currentRow.Exe);
        ExeText.Text   = _currentRow.Exe;

        if (sender is ListView lv && lv.ContainerFromItem(_currentRow) is ListViewItem lvi)
            RowTip.Target = lvi;

        RowTip.IsOpen = true;

        DispatcherQueue.TryEnqueue(() =>
        {
            NameEditor.IsReadOnly = false;
            NameEditor.BorderThickness = new Thickness(1);
            NameEditor.Focus(FocusState.Programmatic);
            NameEditor.SelectAll();
        });
    }
    private void RowTip_Closing(TeachingTip sender, TeachingTipClosingEventArgs e)
        => CommitNameIfNeeded();
    private void RowTip_Opened(TeachingTip sender, object args)
    {
        NameEditor.IsReadOnly = false;
        NameEditor.BorderThickness = new Thickness(1);
        NameEditor.Focus(FocusState.Programmatic);
        NameEditor.SelectAll();
    }
    private void RowTip_ActionButtonClick(TeachingTip sender, object args)
    {
        if (_currentRow == null) return;
        var cfg = AppConfig.Load();
        if (!cfg.ExcludedApps.Contains(_currentRow.Exe, StringComparer.OrdinalIgnoreCase))
        {
            cfg.ExcludedApps.Add(_currentRow.Exe);
            cfg.NormalizeExcludedApps();
            cfg.Save();
        }
        _ = ViewModel.RefreshAsync();
        sender.IsOpen = false;
    }

    private void RowTip_CloseButtonClick(TeachingTip sender, object args)
    {
        CommitNameIfNeeded();
        sender.IsOpen = false;
    }

    private void NameEditor_Tapped(object sender, TappedRoutedEventArgs e)
    {
        NameEditor.IsReadOnly = false;
        NameEditor.BorderThickness = new Thickness(1);
        NameEditor.Focus(FocusState.Programmatic);
        NameEditor.SelectAll();
        e.Handled = true;
    }

    private void NameEditor_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) { CommitNameIfNeeded(); e.Handled = true; }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            if (_currentRow != null)
                NameEditor.Text = AppConfig.Load().GetDisplayNameOrExe(_currentRow.Exe);
            NameEditor.IsReadOnly = true;
            NameEditor.BorderThickness = new Thickness(0);
        }
    }

    private void NameEditor_LostFocus(object sender, RoutedEventArgs e)
        => CommitNameIfNeeded();

    private void CommitNameIfNeeded()
    {
        if (_currentRow == null) return;

        var cfg = AppConfig.Load();
        cfg.SetDisplayName(_currentRow.Exe, NameEditor.Text);

        _ = ViewModel.RefreshAsync();

        NameEditor.IsReadOnly = true;
        NameEditor.BorderThickness = new Thickness(0);
    }
    
    private async void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UiState.SelectedDate))
        {
            StatsDate.SelectedDate = App.State.SelectedDate;
            await ViewModel.RefreshAsync();
        }
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
    public string ExeRaw { get; set; } = "";
    public string DisplayName { get; set; } = "";
}