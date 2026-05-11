using System.Windows;
using System.Windows.Media;
using NovaPad.Core.Interfaces;

namespace NovaPad.WPF.Services;

public class ThemeService : IThemeService
{
    private readonly IAppSettingsService _settings;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public bool IsDarkTheme { get; private set; } = true;
    public string CurrentAccentColor { get; private set; } = "#00BCD4";

    public ThemeService(IAppSettingsService settings)
    {
        _settings = settings;
        IsDarkTheme = _settings.Settings.Theme.IsDark;
        CurrentAccentColor = _settings.Settings.Theme.AccentColor;
        SetThemeResources(IsDarkTheme);
    }

    public void SetTheme(bool isDark)
    {
        IsDarkTheme = isDark;
        _settings.Settings.Theme.IsDark = isDark;
        _settings.Save();
        SetThemeResources(isDark);
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs { IsDark = isDark, AccentColor = CurrentAccentColor });
    }

    public void SetAccentColor(string hexColor)
    {
        CurrentAccentColor = hexColor;
        _settings.Settings.Theme.AccentColor = hexColor;
        _settings.Save();
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        Application.Current.Resources["PrimaryColor"] = color;
        Application.Current.Resources["PrimaryBrush"] = new SolidColorBrush(color);
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs { IsDark = IsDarkTheme, AccentColor = hexColor });
    }

    public void ToggleTheme()
    {
        SetTheme(!IsDarkTheme);
    }

    private static void SetThemeResources(bool isDark)
    {
        Color bg, surface, surfaceAlt, border, text, textSec, textDisabled;

        if (isDark)
        {
            bg = Color.FromRgb(0x0D, 0x0D, 0x0D);
            surface = Color.FromRgb(0x1A, 0x1A, 0x1A);
            surfaceAlt = Color.FromRgb(0x24, 0x24, 0x24);
            border = Color.FromRgb(0x33, 0x33, 0x33);
            text = Color.FromRgb(0xF5, 0xF5, 0xF5);
            textSec = Color.FromRgb(0x9E, 0x9E, 0x9E);
            textDisabled = Color.FromRgb(0x61, 0x61, 0x61);
        }
        else
        {
            bg = Color.FromRgb(0xF5, 0xF5, 0xF5);
            surface = Color.FromRgb(0xFF, 0xFF, 0xFF);
            surfaceAlt = Color.FromRgb(0xE8, 0xE8, 0xE8);
            border = Color.FromRgb(0xD0, 0xD0, 0xD0);
            text = Color.FromRgb(0x1A, 0x1A, 0x1A);
            textSec = Color.FromRgb(0x75, 0x75, 0x75);
            textDisabled = Color.FromRgb(0xBD, 0xBD, 0xBD);
        }

        Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(bg);
        Application.Current.Resources["SurfaceBrush"] = new SolidColorBrush(surface);
        Application.Current.Resources["SurfaceAltBrush"] = new SolidColorBrush(surfaceAlt);
        Application.Current.Resources["BorderBrush"] = new SolidColorBrush(border);
        Application.Current.Resources["TextPrimaryBrush"] = new SolidColorBrush(text);
        Application.Current.Resources["TextSecondaryBrush"] = new SolidColorBrush(textSec);
        Application.Current.Resources["TextDisabledBrush"] = new SolidColorBrush(textDisabled);

        Application.Current.Resources["SidebarGradient"] = new LinearGradientBrush(
            surface, bg, new Point(0, 0), new Point(1, 0));
    }
}
