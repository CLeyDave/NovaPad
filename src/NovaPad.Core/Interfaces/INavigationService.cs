namespace NovaPad.Core.Interfaces;

public interface INavigationService
{
    event EventHandler<NavigationEventArgs>? NavigationChanged;

    object? CurrentView { get; }
    string CurrentViewName { get; }
    void NavigateTo<TView>() where TView : class;
    void NavigateTo(string viewName);
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    void GoBack();
    void GoForward();
}

public class NavigationEventArgs : EventArgs
{
    public string ViewName { get; set; } = string.Empty;
    public object? ViewInstance { get; set; }
    public object? Parameter { get; set; }
}
