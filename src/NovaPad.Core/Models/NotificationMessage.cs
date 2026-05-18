namespace NovaPad.Core.Models;

public class NotificationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);
    public bool IsDismissed { get; set; }
    public Action? OnClick { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    BatteryLow,
    DeviceConnected,
    DeviceDisconnected,
    ProfileChanged
}
