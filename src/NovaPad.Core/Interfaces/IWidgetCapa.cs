namespace NovaPad.Core.Interfaces;

public interface IWidgetCapa
{
    string Clave { get; }
    string Titulo { get; }
    bool Visible { get; set; }
    void Refrescar();
    object? ObtenerVista();
}
