using System.Globalization;
using System.Windows.Data;
using NovaPad.Core.Enums;

namespace NovaPad.WPF.Converters;

public class ButtonTypeDisplayConverter : IValueConverter
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
                ButtonType.DPadUp => "\u25B2  D-Pad Up",
                ButtonType.DPadDown => "\u25BC  D-Pad Down",
                ButtonType.DPadLeft => "\u25C0  D-Pad Left",
                ButtonType.DPadRight => "\u25B6  D-Pad Right",
                ButtonType.LeftBumper => "LB  Left Bumper",
                ButtonType.RightBumper => "RB  Right Bumper",
                ButtonType.LeftTrigger => "LT  Left Trigger",
                ButtonType.RightTrigger => "RT  Right Trigger",
                ButtonType.LeftStick => "LS  Left Stick",
                ButtonType.RightStick => "RS  Right Stick",
                ButtonType.LeftStickUp => "LS \u2191",
                ButtonType.LeftStickDown => "LS \u2193",
                ButtonType.LeftStickLeft => "LS \u2190",
                ButtonType.LeftStickRight => "LS \u2192",
                ButtonType.RightStickUp => "RS \u2191",
                ButtonType.RightStickDown => "RS \u2193",
                ButtonType.RightStickLeft => "RS \u2190",
                ButtonType.RightStickRight => "RS \u2192",
                ButtonType.L3 => "L3  Left Stick Click",
                ButtonType.R3 => "R3  Right Stick Click",
                ButtonType.Start => "Start",
                ButtonType.Back => "Back",
                ButtonType.Guide => "\u25CF  Guide",
                ButtonType.Share => "Share",
                ButtonType.Options => "Options",
                ButtonType.TouchpadClick => "Touchpad",
                ButtonType.Gyro => "Gyro",
                ButtonType.Accelerometer => "Accelerometer",
                ButtonType.Custom => "Custom",
                _ => bt.ToString()
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
