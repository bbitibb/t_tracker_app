using System;
using Microsoft.UI.Xaml.Data;

namespace t_tracker_ui.Util;

public sealed class SecondsToHmsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double secs)
            return TimeSpan.FromSeconds(secs).ToString(@"hh\:mm\:ss");
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}