using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NovaPad.WPF.Converters;

public class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    var r = byte.Parse(hex[..2], NumberStyles.HexNumber);
                    var g = byte.Parse(hex[2..4], NumberStyles.HexNumber);
                    var b = byte.Parse(hex[4..6], NumberStyles.HexNumber);
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
                }
            }
            catch { }
        }
        return new SolidColorBrush(Color.FromRgb(0, 188, 212));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
