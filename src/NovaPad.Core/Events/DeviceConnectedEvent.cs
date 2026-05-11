namespace NovaPad.Core.Events;

public class DeviceConnectedEvent
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int BatteryLevel { get; set; } = -1;
    public bool IsCharging { get; set; }
}
