using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NovaPad.WPF.Converters;

public class ButtonActiveBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool active && active)
            return new SolidColorBrush(Color.FromRgb(0x00, 0xBC, 0xD4));
        return new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ButtonActiveFgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool active && active)
            return Brushes.White;
        return new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BatteryPercentColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double level)
        {
            if (level <= 0) return new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
            if (level <= 0.15) return new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
            if (level <= 0.30) return new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
            if (level <= 0.60) return new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0x3B));
            return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        }
        return new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
