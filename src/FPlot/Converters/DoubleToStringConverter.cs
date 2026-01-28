using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace FPlot.Converters;

public class DoubleToStringConverter : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null)
        {
            return string.Empty;
        }
        if (targetType == typeof(string)) {
            if (value is double) {
                var d = (double)value;
                if (!double.IsNaN(d)) {
                    return $"{d:F9}";
                }
                return "N/A";
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return 0;
        if (value is string s)
        {
            if (double.TryParse(s, out var d))
            {
                return d;
            }
        }
        return 0;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
