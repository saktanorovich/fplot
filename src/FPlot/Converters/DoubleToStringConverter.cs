using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using FPlot.ViewModels;

namespace FPlot.Converters;

public class DoubleToStringConverter : MarkupExtension, IValueConverter
{
    // Fixed-point format the grid displays (and parses back) values in.
    public const string Format = PointViewModel.DisplayFormat;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is null)
        {
            return string.Empty;
        }
        if (targetType == typeof(string)) {
            if (value is double) {
                var d = (double)value;
                if (!double.IsNaN(d)) {
                    return d.ToString(Format, culture);
                }
                return "N/A";
            }
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, NumberStyles.Float, culture, out var d))
        {
            return d;
        }
        // Keep the current value while the input is not a valid number.
        return BindingOperations.DoNothing;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
