using HidSharp;
using NovaPad.Core.Interfaces;

namespace NovaPad.HID.Services;

public class HidDirectBatteryService : IBatteryService, IDisposable
{
    private bool _disposed;

    public bool IsAvailable => !_disposed;

    public BatteryResult? GetBatteryLevel(ushort vendorId, ushort productId)
    {
        if (_disposed) return null;

        try
        {
            var devices = DeviceList.Local.GetHidDevices()
                .Where(d => (ushort)d.VendorID == vendorId && (ushort)d.ProductID == productId)
                .ToList();

            foreach (var device in devices)
            {
                try
                {
                    var result = TryReadBattery(device);
                    if (result != null && result.Level >= 0)
                        return result;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static BatteryResult? TryReadBattery(HidDevice device)
    {
        var vid = (ushort)device.VendorID;
        var pid = (ushort)device.ProductID;

        // Try feature report approach first (DualSense)
        if (vid == 0x054C && pid is >= 0x0CE6 and <= 0x0E0B)
            return ReadDualSenseFeatureBattery(device);

        // DS4 via feature report
        if (vid == 0x054C && pid is >= 0x09CC and <= 0x0BA0)
            return ReadDualShock4FeatureBattery(device);

        return null;
    }

    private static BatteryResult? ReadDualSenseFeatureBattery(HidDevice device)
    {
        try
        {
            using var stream = device.Open();
            var buf = new byte[64];

            buf[0] = 0x09;
            stream.GetFeature(buf);

            if (buf[0] == 0x09 && buf.Length > 27)
            {
                var val = buf[27];
                var level = (val & 0x0F) * 100 / 10;
                var charging = (val & 0x10) != 0;
                if (level > 0 || val != 0)
                {
                    return new BatteryResult
                    {
                        Level = Math.Clamp(level, 0, 100),
                        IsCharging = charging,
                        Source = BatterySource.HidReport
                    };
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static BatteryResult? ReadDualShock4FeatureBattery(HidDevice device)
    {
        try
        {
            using var stream = device.Open();
            var buf = new byte[64];

            buf[0] = 0x05;
            stream.GetFeature(buf);

            if (buf[0] == 0x05 && buf.Length > 30)
            {
                var val = buf[30];
                var level = (val & 0x0F) * 100 / 10;
                var charging = (val & 0x10) != 0;
                return new BatteryResult
                {
                    Level = Math.Clamp(level, 0, 100),
                    IsCharging = charging,
                    Source = BatterySource.HidReport
                };
            }
        }
        catch
        {
        }

        return null;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
