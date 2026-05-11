using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NovaPad.WPF.Converters;

public class BatteryLevelToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int level)
        {
            if (level < 0) return new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
            if (level <= 15) return new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
            if (level <= 30) return new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
            if (level <= 60) return new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0x3B));
            return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        }
        return new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
