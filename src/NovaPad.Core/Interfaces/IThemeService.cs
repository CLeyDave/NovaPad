namespace NovaPad.Core.Interfaces;

public interface IThemeService
{
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    bool IsDarkTheme { get; }
    string CurrentAccentColor { get; }
    void SetTheme(bool isDark);
    void SetAccentColor(string hexColor);
    void ToggleTheme();
}

public class ThemeChangedEventArgs : EventArgs
{
    public bool IsDark { get; set; }
    public string AccentColor { get; set; } = "#00BCD4";
}
