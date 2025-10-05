using System;
using System.ComponentModel;
using System.Linq;
using Windows.UI;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using t_tracker_ui.Services;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Measure;
using Microsoft.UI.Xaml.Navigation;
using t_tracker_app.core;
using t_tracker_ui.State;
using t_tracker_ui.ViewModels;
using Microsoft.UI.Xaml;


namespace t_tracker_ui.Views;

public sealed partial class ChartsPage : Page
{
    private readonly StatsReader _stats = new();
    private readonly DispatcherTimer _autoTimer = new();
    public DashboardViewModel ViewModel { get; } = new();

    private readonly ColumnSeries<double> _series = new()
    {
        AnimationsSpeed = TimeSpan.Zero,
        EasingFunction  = null
    };
    
    public ChartsPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        
        StatsDate.Date = App.State.SelectedDate;
        TopNBox.Value = App.State.Limit;

        _autoTimer.Interval = TimeSpan.FromSeconds(1);
        _autoTimer.Tick += (_, __) =>
        {
            var selected = DateOnly.FromDateTime(StatsDate.Date.DateTime);
            if (selected == DateOnly.FromDateTime(DateTime.Now))
                LoadAndRender();
        };
        
        DataContext = ViewModel;
        
        App.State.PropertyChanged += OnAppStateChanged;

        StatsDate.DateChanged += (_, args) =>
        {
            App.State.SelectedDate = args.NewDate;
        };
        
        Chart.Series = new ISeries[] { _series, };
        _series.Fill = new LinearGradientPaint(
            new[] { SKColors.Teal, SKColors.Black },
            new SKPoint(0, 0), new SKPoint(0, 1));
        _series.Stroke = new SolidColorPaint(SKColors.Gray)
        {
            StrokeThickness = 1.2f,
            IsAntialias = true 
        };
        
        App.State.PropertyChanged += OnAppStateChanged;
        StatsDate.DateChanged += (_, __) => LoadAndRender();
        TopNBox.ValueChanged += (_, __) => LoadAndRender();
        LoadAndRender();
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _autoTimer.Start(); 
        LoadAndRender();
        
        StatsDate.Date = App.State.SelectedDate;
    }
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _autoTimer.Stop();
    }
    private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UiState.SelectedDate))
        {
            StatsDate.Date = App.State.SelectedDate;
            LoadAndRender();
        }
    }
    private void LoadAndRender()
    {
        var day = DateOnly.FromDateTime(StatsDate.Date.DateTime);
        var n = Math.Max(1, (int)TopNBox.Value);
        AppConfig config = AppConfig.Load();

        var (_, top) = _stats.LoadDay(day, n);
        var data = top
            .Where(u => !config.IsExcludedApp(u.exe) && u.exe != "Idle" && u.exe != "Stopped" && u.exe != "Excluded")
            .OrderByDescending(u => u.secs)
            .Take(n)
            .Select((u, i) => new UsageRowVm
            {
                Rank = i + 1,
                Exe = u.exe,
                Seconds = u.secs
            }); ;

        var labels = data.Select(r => r.Exe).ToArray();
        var values = data.Select(r => r.Seconds).ToArray();

        var boldAxisPaint = new SolidColorPaint(new SKColor(0x33, 0x33, 0x33))
        {
            SKTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
        };

        var redAxisPaint = new SolidColorPaint(SKColors.Firebrick)
        {
            SKTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
        };

        
        _series.Values = values;
        Chart.XAxes = new[]
        {
            new Axis
            {
                Labels = labels,
                MinStep = 1,
                ForceStepToMin = true,
                LabelsRotation = 315,
                TextSize = 15,
                LabelsPaint = boldAxisPaint
            }
        };
        Chart.Padding = new Thickness(0, 0, 0, 28);
        Chart.YAxes = new[]
        {
            new Axis
            {
                Labeler = v =>
                {
                    var ts = TimeSpan.FromSeconds(v);
                    int h = (int)ts.TotalHours;
                    int m = ts.Minutes;

                    if (h > 0 && m > 0) return $"{h}h {m}m";
                    if (h > 0)          return $"{h}h";
                    if (m > 0)          return $"{m}m";
                    return $"{ts.Seconds}s";
                },
                TextSize = 15,
                LabelsPaint = new SolidColorPaint(SKColors.DimGray)
                {
                    SKTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
                }
            }
        };
        
    }
}
