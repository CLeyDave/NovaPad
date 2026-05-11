using System.Windows;
using NovaPad.Overlay.Themes;

namespace NovaPad.Overlay.Widgets;

public class DebugWidgetImpl : WidgetBase
{
    private readonly ThemeManager _theme;
    private DebugWidget? _view;

    public override string Id => "debug";
    public override string Title => "Debug";

    public DebugWidgetImpl(ThemeManager theme)
    {
        _theme = theme;
        IsVisible = false;
    }

    protected override FrameworkElement CreateView()
    {
        _view = new DebugWidget();
        return _view;
    }

    protected override void OnUpdate()
    {
        _view?.Tick(DateTime.Now.TimeOfDay.TotalSeconds);
    }

    protected override void OnThemeChanged(bool isDark, string? accentColor, double opacity)
    {
    }

    public void SetLatency(double ms)
    {
        _view?.SetLatency(ms);
    }
}
