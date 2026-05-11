namespace NovaPad.Core.Interfaces;

public enum BatterySource { Unknown, HidReport, Sdl2, WindowsGaming }

public class BatteryResult
{
    public int Level { get; set; } = -1;
    public bool IsCharging { get; set; }
    public BatterySource Source { get; set; } = BatterySource.Unknown;
}

public interface IBatteryService
{
    BatteryResult? GetBatteryLevel(ushort vendorId, ushort productId);
    bool IsAvailable { get; }
}
