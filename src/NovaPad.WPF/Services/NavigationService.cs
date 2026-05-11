using System.Windows.Controls;
using NovaPad.Core.Interfaces;

namespace NovaPad.WPF.Services;

public class NavigationService : INavigationService
{
    public event EventHandler<NavigationEventArgs>? NavigationChanged;

    public object? CurrentView { get; private set; }
    public string CurrentViewName { get; private set; } = string.Empty;

    private readonly Dictionary<string, Func<object>> _viewFactories = new();
    private readonly Stack<(string name, object? view)> _backStack = new();
    private readonly Stack<(string name, object? view)> _forwardStack = new();
    private ContentControl? _contentControl;

    public void RegisterView(string name, Func<object> factory)
    {
        _viewFactories[name] = factory;
    }

    public void SetContentControl(ContentControl control)
    {
        _contentControl = control;
    }

    public void NavigateTo<TView>() where TView : class
    {
        var name = typeof(TView).Name;
        NavigateTo(name);
    }

    public void NavigateTo(string viewName)
    {
        if (!_viewFactories.TryGetValue(viewName, out var factory)) return;

        if (CurrentView != null)
        {
            _backStack.Push((CurrentViewName, CurrentView));
            _forwardStack.Clear();
        }

        var view = factory();
        CurrentView = view;
        CurrentViewName = viewName;

        if (_contentControl != null)
        {
            _contentControl.Content = view;
        }

        NavigationChanged?.Invoke(this, new NavigationEventArgs
        {
            ViewName = viewName,
            ViewInstance = view
        });
    }

    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;

    public void GoBack()
    {
        if (!CanGoBack) return;
        if (CurrentView != null)
            _forwardStack.Push((CurrentViewName, CurrentView));

        var (name, view) = _backStack.Pop();
        CurrentView = view;
        CurrentViewName = name;

        if (_contentControl != null)
            _contentControl.Content = view;

        NavigationChanged?.Invoke(this, new NavigationEventArgs { ViewName = name, ViewInstance = view });
    }

    public void GoForward()
    {
        if (!CanGoForward) return;
        if (CurrentView != null)
            _backStack.Push((CurrentViewName, CurrentView));

        var (name, view) = _forwardStack.Pop();
        CurrentView = view;
        CurrentViewName = name;

        if (_contentControl != null)
            _contentControl.Content = view;

        NavigationChanged?.Invoke(this, new NavigationEventArgs { ViewName = name, ViewInstance = view });
    }
}
