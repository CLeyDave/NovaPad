using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NovaPad.WPF.Converters;

public class SidebarWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool expanded)
        {
            return expanded ? new GridLength(220) : new GridLength(60);
        }
        return new GridLength(60);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
