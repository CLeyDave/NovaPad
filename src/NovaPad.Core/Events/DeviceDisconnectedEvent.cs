namespace NovaPad.Core.Events;

public class DeviceDisconnectedEvent
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
