using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NovaPad.WPF.Converters;

public class RatioToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ratio = value is double d ? Math.Clamp(d, 0, 1) : 0d;
        return new GridLength(ratio * 100, GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseRatioToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ratio = value is double d ? Math.Clamp(d, 0, 1) : 0d;
        return new GridLength((1 - ratio) * 100, GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
