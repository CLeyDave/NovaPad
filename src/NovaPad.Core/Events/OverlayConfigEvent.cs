namespace NovaPad.Core.Events;

public class OverlayConfigEvent
{
    public double Opacity { get; set; } = 0.8;
    public bool ShowBattery { get; set; } = true;
    public bool ShowLatency { get; set; } = true;
    public bool ShowPollingRate { get; set; }
    public bool ShowProfileName { get; set; } = true;
    public bool ShowConnectionStatus { get; set; } = true;
    public bool ShowFps { get; set; }
    public bool ShowControllerType { get; set; } = true;
    public string HudAnchor { get; set; } = "TopRight";
    public double HudOffsetX { get; set; }
    public double HudOffsetY { get; set; }
    public string NotificationAnchor { get; set; } = "BottomRight";
    public double NotificationOffsetX { get; set; } = 24;
    public double NotificationOffsetY { get; set; } = 80;
    public bool ShowNotifications { get; set; } = true;
}
