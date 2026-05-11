using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NovaPad.Core.Enums;

namespace NovaPad.WPF.Converters;

public class MappingTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MappingType mt && parameter is string s)
            return mt.ToString() == s;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter is string s)
        {
            if (Enum.TryParse<MappingType>(s, out var mt))
                return mt;
        }
        return Binding.DoNothing;
    }
}

public class MappingVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MappingType mt && parameter is string s)
            return mt.ToString() == s ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
