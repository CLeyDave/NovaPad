namespace NovaPad.Core.Models;

public class AppSettings
{
    public ThemeSettings Theme { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public OverlayConfig Overlay { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool AutoStartOverlay { get; set; } = true;
}

public class ThemeSettings
{
    public bool IsDark { get; set; } = true;
    public string AccentColor { get; set; } = "#00BCD4";
}

public class WindowSettings
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; } = 1280;
    public double Height { get; set; } = 820;
    public bool IsMaximized { get; set; } = true;
    public bool SidebarExpanded { get; set; } = true;
}

public class NotificationSettings
{
    public double DurationSeconds { get; set; } = 3.0;
    public int BatteryWarningThreshold { get; set; } = 20;
    public bool ShowConnectionNotifications { get; set; } = true;
    public bool ShowBatteryNotifications { get; set; } = true;
}
