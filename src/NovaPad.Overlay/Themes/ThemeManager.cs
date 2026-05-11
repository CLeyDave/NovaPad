using System.Windows;
using System.Windows.Media;
using NovaPad.Core.Events;

namespace NovaPad.Overlay.Themes;

public class ThemeManager
{
    private bool _isDark = true;
    private Color _accentColor = Color.FromRgb(0, 188, 212);
    private double _backgroundOpacity = 0.8;

    public bool IsDark => _isDark;
    public Color AccentColor => _accentColor;
    public double BackgroundOpacity => _backgroundOpacity;

    public event Action<ThemeChangedEvent>? ThemeChanged;

    public void ApplyTheme(ThemeChangedEvent theme)
    {
        _isDark = theme.IsDark;
        _backgroundOpacity = theme.BackgroundOpacity;

        if (!string.IsNullOrEmpty(theme.AccentColor))
        {
            try
            {
                _accentColor = (Color)ColorConverter.ConvertFromString(theme.AccentColor);
            }
            catch { }
        }

        ThemeChanged?.Invoke(theme);
    }

    public Color GetBatteryColor(int level)
    {
        if (level < 0) return Color.FromRgb(170, 170, 170);
        if (level <= 20) return Color.FromRgb(244, 67, 54);
        if (level <= 70) return Color.FromRgb(255, 193, 7);
        return Color.FromRgb(76, 175, 80);
    }

    public SolidColorBrush GetSurfaceBrush()
    {
        var alpha = (byte)(_backgroundOpacity * 255);
        var baseColor = _isDark ? Color.FromRgb(17, 17, 17) : Color.FromRgb(245, 245, 245);
        return new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
    }

    public SolidColorBrush GetTextBrush()
    {
        return new SolidColorBrush(_isDark ? Colors.White : Colors.Black);
    }

    public SolidColorBrush GetMutedBrush()
    {
        var c = _isDark ? Color.FromRgb(170, 170, 170) : Color.FromRgb(100, 100, 100);
        return new SolidColorBrush(c);
    }
}
