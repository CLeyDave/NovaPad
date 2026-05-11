namespace NovaPad.Core.Events;

public class ThemeChangedEvent
{
    public bool IsDark { get; set; } = true;
    public string? AccentColor { get; set; }
    public double BackgroundOpacity { get; set; } = 0.8;
}
