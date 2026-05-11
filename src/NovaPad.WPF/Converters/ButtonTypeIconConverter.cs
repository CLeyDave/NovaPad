using System.Globalization;
using System.Windows.Data;
using NovaPad.Core.Enums;

namespace NovaPad.WPF.Converters;

public class ButtonTypeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ButtonType bt)
        {
            return bt switch
            {
                ButtonType.A => "A",
                ButtonType.B => "B",
                ButtonType.X => "X",
                ButtonType.Y => "Y",
                ButtonType.DPadUp => "\u25B2",
                ButtonType.DPadDown => "\u25BC",
                ButtonType.DPadLeft => "\u25C0",
                ButtonType.DPadRight => "\u25B6",
                ButtonType.LeftBumper => "LB",
                ButtonType.RightBumper => "RB",
                ButtonType.LeftTrigger => "LT",
                ButtonType.RightTrigger => "RT",
                ButtonType.LeftStick => "LS",
                ButtonType.RightStick => "RS",
                ButtonType.L3 => "L3",
                ButtonType.R3 => "R3",
                ButtonType.Start => "\u23F5",
                ButtonType.Back => "\u23F4",
                ButtonType.Guide => "\u25CF",
                ButtonType.Share => "\u21C4",
                ButtonType.Options => "\u2699",
                ButtonType.TouchpadClick => "\u2714",
                _ => "\u2022"
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
