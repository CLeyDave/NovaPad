using NovaPad.Core.Models;

namespace NovaPad.Core.Interfaces;

public interface IOverlayService
{
    event EventHandler<OverlayConfig>? ConfigChanged;

    bool IsVisible { get; }
    OverlayConfig Config { get; }
    Task ShowAsync();
    Task HideAsync();
    Task UpdateAsync(ControllerState? state, ControllerInfo? info);
    Task UpdateConfigAsync(OverlayConfig config);
}
