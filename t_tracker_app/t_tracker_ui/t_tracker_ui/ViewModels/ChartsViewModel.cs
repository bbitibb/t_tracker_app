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

        };
    }
}