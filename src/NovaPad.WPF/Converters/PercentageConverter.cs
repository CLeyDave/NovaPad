using System.Globalization;
using System.Windows.Data;

namespace NovaPad.WPF.Converters;

public class PercentageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return $"{d * 100:F0}%";
        if (value is int i)
            return $"{i}%";
        return "0%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
