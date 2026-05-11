using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NovaPad.Core.Enums;

namespace NovaPad.WPF.Converters;

public class ConnectionTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is ConnectionType type
            ? type switch
            {
                ConnectionType.Usb => "UsbIcon",
                ConnectionType.Bluetooth => "BluetoothIcon",
                ConnectionType.WirelessAdapter => "GamepadIcon",
                ConnectionType.Wired => "UsbIcon",
                _ => "UsbIcon"
            }
            : "UsbIcon";

        return Application.Current.FindResource(key) is Geometry geometry
            ? geometry
            : Geometry.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
