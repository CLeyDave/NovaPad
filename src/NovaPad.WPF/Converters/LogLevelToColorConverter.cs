using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using NovaPad.Core.Enums;

namespace NovaPad.WPF.Converters;

public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61)),
                LogLevel.Debug => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)),
                LogLevel.Info => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                LogLevel.Warning => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),
                LogLevel.Error => new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)),
                LogLevel.Fatal => new SolidColorBrush(Color.FromRgb(0xE9, 0x1E, 0x63)),
                _ => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E))
            };
        }
        return new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
