namespace NovaPad.Core.Events;

public class BatteryChangedEvent
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsCharging { get; set; }
}
