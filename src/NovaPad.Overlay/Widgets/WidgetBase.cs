using System.Windows;
using System.Windows.Controls;
using NovaPad.Core.Interfaces;

namespace NovaPad.Overlay.Widgets;

public abstract class WidgetBase : IOverlayWidget
{
    public abstract string Id { get; }
    public abstract string Title { get; }
    public bool IsVisible { get; set; } = true;

    private FrameworkElement? _view;

    public object? GetView()
    {
        if (_view == null)
            _view = CreateView();
        return _view;
    }

    public void Update()
    {
        if (!IsVisible) return;
        if (_view == null || _view.Visibility != Visibility.Visible) return;
        OnUpdate();
    }

    protected abstract FrameworkElement CreateView();
    protected abstract void OnUpdate();
    protected abstract void OnThemeChanged(bool isDark, string? accentColor, double opacity);

    public void ApplyTheme(bool isDark, string? accentColor, double opacity)
    {
        OnThemeChanged(isDark, accentColor, opacity);
    }
}
