namespace NovaPad.Core.Models;

public class OverlayConfig
{
    public bool IsEnabled { get; set; } = true;
    public double X { get; set; } = 100;
    public double Y { get; set; } = 100;
    public double Opacity { get; set; } = 0.8;
    public double Scale { get; set; } = 1.0;
    public bool ClickThrough { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public bool ShowBattery { get; set; } = true;
    public bool ShowControllerType { get; set; } = true;
    public bool ShowLatency { get; set; } = true;
    public bool ShowInputs { get; set; } = true;
    public bool ShowProfileName { get; set; } = true;
    public bool ShowConnectionStatus { get; set; } = true;
    public bool ShowPollingRate { get; set; }
    public bool ShowFps { get; set; }
    public string Theme { get; set; } = "Dark";
    public string Anchor { get; set; } = "TopLeft";
    public bool ShowNotifications { get; set; } = true;
    public string NotificationAnchor { get; set; } = "BottomRight";
    public double NotificationOffsetX { get; set; } = 24;
    public double NotificationOffsetY { get; set; } = 80;
}
