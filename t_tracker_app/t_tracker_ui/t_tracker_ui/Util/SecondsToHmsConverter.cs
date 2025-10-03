using System;
using Microsoft.UI.Xaml.Data;

namespace t_tracker_ui.Util;

public sealed class SecondsToHmsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double seconds = (double)value;

        var ts = TimeSpan.FromSeconds(seconds);
        int h = (int)ts.TotalHours;
        int m = ts.Minutes;

        if (h > 0 && m > 0) return $"{h}h {m}m";
        if (h > 0) return $"{h}h";
        if (m > 0) return $"{m}m";
        return $"{ts.Seconds}s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}