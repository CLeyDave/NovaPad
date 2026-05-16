namespace NovaPad.Core.Models;

public class AppSettings
{
    public ThemeSettings Theme { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public AjustesOverlay Overlay { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public Dictionary<string, RgbSavedState> RgbState { get; set; } = new();
    public Dictionary<string, InputProcessingConfig> InputProcessing { get; set; } = new();
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool AutoStartOverlay { get; set; } = true;
    public string Language { get; set; } = "es";
}

public class RgbSavedState
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte R2 { get; set; }
    public byte G2 { get; set; }
    public byte B2 { get; set; }
    public int EffectIndex { get; set; }
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
