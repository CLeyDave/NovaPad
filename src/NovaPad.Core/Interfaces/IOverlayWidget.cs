namespace NovaPad.Core.Interfaces;

public interface IOverlayWidget
{
    string Id { get; }
    string Title { get; }
    bool IsVisible { get; set; }
    void Update();
    object? GetView();
}
