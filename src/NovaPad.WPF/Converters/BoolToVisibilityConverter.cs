using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NovaPad.WPF.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }
    public bool UseHidden { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        if (IsInverted) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : (UseHidden ? Visibility.Hidden : Visibility.Collapsed);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}
