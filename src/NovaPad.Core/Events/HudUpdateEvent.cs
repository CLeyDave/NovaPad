namespace NovaPad.Core.Events;

public class HudControllerData
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int BatteryLevel { get; set; } = -1;
    public bool IsCharging { get; set; }
    public bool IsConnected { get; set; } = true;
    public double LatencyMs { get; set; }
    public int PollingRateHz { get; set; }
    public string? ProfileName { get; set; }
}

public class HudUpdateEvent
{
    public List<HudControllerData> Controllers { get; set; } = new();
}
