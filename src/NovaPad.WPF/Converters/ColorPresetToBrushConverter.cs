using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NovaPad.WPF.Converters;

public class ColorPresetToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ViewModels.ColorPreset preset)
            return new SolidColorBrush(Color.FromRgb(preset.R, preset.G, preset.B));
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
