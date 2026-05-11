using System.Windows;
using System.Windows.Controls;
using NovaPad.Core.Interfaces;

namespace NovaPad.Overlay.Widgets;

public class WidgetManager
{
    private readonly Dictionary<string, IOverlayWidget> _widgets = new();
    private readonly StackPanel _container;

    public IReadOnlyCollection<IOverlayWidget> Widgets => _widgets.Values.ToList();

    public WidgetManager(StackPanel container)
    {
        _container = container;
    }

    public void Register(IOverlayWidget widget)
    {
        if (_widgets.ContainsKey(widget.Id)) return;
        _widgets[widget.Id] = widget;
        var view = widget.GetView() as FrameworkElement;
        if (view != null)
        {
            view.Visibility = widget.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            _container.Children.Add(view);
        }
    }

    public void SetVisibility(string widgetId, bool visible)
    {
        if (!_widgets.TryGetValue(widgetId, out var widget)) return;
        widget.IsVisible = visible;
        var view = widget.GetView() as FrameworkElement;
        if (view != null)
            view.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public void UpdateAll()
    {
        foreach (var widget in _widgets.Values)
            widget.Update();
    }

    public void ApplyThemeToAll(bool isDark, string? accentColor, double opacity)
    {
        foreach (var widget in _widgets.Values.OfType<WidgetBase>())
            widget.ApplyTheme(isDark, accentColor, opacity);
    }

    public T? GetWidget<T>() where T : class, IOverlayWidget
    {
        return _widgets.Values.OfType<T>().FirstOrDefault();
    }
}
