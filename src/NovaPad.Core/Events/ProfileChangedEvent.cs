namespace NovaPad.Core.Events;

public class ProfileChangedEvent
{
    public string DeviceId { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
}
