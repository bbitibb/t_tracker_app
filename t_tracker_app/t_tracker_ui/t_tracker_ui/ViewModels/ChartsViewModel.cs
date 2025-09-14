using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Kernel;

namespace t_tracker_ui.ViewModels;

public class ChartsViewModel
{
    public ISeries[] AppUsageSeries { get; }

    public ChartsViewModel()
    {
        AppUsageSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Minutes",
                Values = new double[] { 30, 45, 10, 60 },
                DataLabelsPaint = new SolidColorPaint(SKColors.Gray),
                DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N0}m"
            }
        };
    }
}