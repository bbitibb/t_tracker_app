using System;
using System.ComponentModel;
using System.Linq;
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
using t_tracker_ui.State;

namespace t_tracker_ui.Views;

public sealed partial class ChartsPage : Page
{
    private readonly StatsReader _stats = new();

    public ChartsPage()
    {
        InitializeComponent();
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        StatsDate.Date = DateTimeOffset.Now;
        TopNBox.Value  = 10;
        
        App.State.PropertyChanged += OnAppStateChanged;
        StatsDate.DateChanged += (_, __) => LoadAndRender();
        TopNBox.ValueChanged  += (_, __) => LoadAndRender();
        LoadAndRender();
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LoadAndRender();
    }
    private void OnRefresh(object sender, RoutedEventArgs e) => LoadAndRender();
    private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UiState.SelectedDate))
            LoadAndRender();
    }
    private void LoadAndRender()
    {
        var day = DateOnly.FromDateTime(StatsDate.Date.DateTime);
        var n   = (int)(TopNBox.Value > 0 ? TopNBox.Value : 10);

        var (_, top) = _stats.LoadDay(day, n);
        var data     = top.ToList();

        var labels = data.Select(r => r.exe).ToArray();
        var values = data.Select(r => r.secs).ToArray();

        var boldAxisPaint = new SolidColorPaint(new SKColor(0x33, 0x33, 0x33))  // dark gray
        {
            SKTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
        };

        var redAxisPaint = new SolidColorPaint(SKColors.Firebrick)
        {
            SKTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
        };
        
        Chart.Series = new ISeries[]
        {
            new ColumnSeries<double> { Values = values }
        };

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
                Labeler = v => TimeSpan.FromSeconds(v).ToString(@"hh\:mm"),
                TextSize = 15,
                LabelsPaint = new SolidColorPaint(SKColors.DimGray)
                {
                    SKTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
                }
            }
        };
    }
}
