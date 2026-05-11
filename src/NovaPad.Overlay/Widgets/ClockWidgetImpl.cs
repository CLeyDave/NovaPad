using System.Windows;
using System.Windows.Media;
using NovaPad.Core.Interfaces;
using NovaPad.Overlay.Themes;

namespace NovaPad.Overlay.Widgets;

public class ClockWidgetImpl : WidgetBase
{
    private readonly ThemeManager _theme;
    private ClockWidget? _view;

    public override string Id => "clock";
    public override string Title => "Reloj";

    public ClockWidgetImpl(ThemeManager theme)
    {
        _theme = theme;
    }

    protected override FrameworkElement CreateView()
    {
        _view = new ClockWidget();
        OnThemeChanged(_theme.IsDark, null, _theme.BackgroundOpacity);
        return _view;
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnThemeChanged(bool isDark, string? accentColor, double opacity)
    {
        if (_view == null) return;
        _view.ClockText.Foreground = _theme.GetTextBrush();
    }
}
