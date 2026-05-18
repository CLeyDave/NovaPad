using System.Threading.Tasks;
using NovaPad.Core.Events;

namespace NovaPad.Core.Interfaces;

public interface IOverlayService
{
    bool IsActive { get; }
    void Show();
    void Hide();
    void ApplyConfig(DatosConfigOverlay config);
    void ApplyCardConfig(OverlayCardConfig config);
    void UpdateHud(InformeMandos hud);
    void ShowNotification(string title, string message);
    void NotifyConnection(EstadoConexion ev);
    void NotifyTheme(CambioTema ev);
    void TogglePanel();
    Task WaitUntilReadyAsync();
    void ResetReadyState();
}
