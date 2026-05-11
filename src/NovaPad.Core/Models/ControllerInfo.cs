using NovaPad.Core.Enums;

namespace NovaPad.Core.Models;

public class ControllerInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DisplayName { get; set; } = "Unknown Controller";
    public string CustomName { get; set; } = string.Empty;
    public ControllerType Type { get; set; } = ControllerType.Unknown;
    public ConnectionType Connection { get; set; } = ConnectionType.Unknown;
    public string Manufacturer { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public ushort VendorId { get; set; }
    public ushort ProductId { get; set; }
    public int BatteryLevel { get; set; } = -1;
    public bool IsCharging { get; set; }
    public double LatencyMs { get; set; }
    public double SignalStrength { get; set; }
    public bool IsConnected { get; set; } = true;
    public bool IsEmulated { get; set; }
    public string AssignedProfileId { get; set; } = string.Empty;
    public int PollingRateHz { get; set; }
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public TimeSpan TotalUptime { get; set; }
    public string? IconPath { get; set; }
    public string RgbColor { get; set; } = "#00BCD4";
    public bool HasTouchpad { get; set; }
    public bool HasGyroscope { get; set; }
    public bool HasAccelerometer { get; set; }
    public bool HasRumble { get; set; }
    public bool HasLed { get; set; }
    public bool HasBattery { get; set; }
    public string? HidDevicePath { get; set; }
    public int MaxInputReportLength { get; set; }
    public int MaxFeatureReportLength { get; set; }

    public string EffectiveName => string.IsNullOrEmpty(CustomName) ? DisplayName : CustomName;
}
