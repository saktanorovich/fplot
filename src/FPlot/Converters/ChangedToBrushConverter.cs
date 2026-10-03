using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace FPlot.Converters;

// Colors an edited (unsaved) value differently from a saved one.
public class ChangedToBrushConverter : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Brushes.LightCoral : Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
