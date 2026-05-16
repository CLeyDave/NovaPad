using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovaPad.WPF.Overlay.Dibujo;

namespace NovaPad.WPF.Overlay.Partes;

public class CuadroDepuracion
{
    private readonly TextBlock _texto;

    public UIElement Vista => _texto;

    public CuadroDepuracion()
    {
        _texto = new TextBlock
        {
            FontSize = 10,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, 8, 0, 0)
        };
    }

    public void Marcar(double fps, double latenciaMs)
    {
        _texto.Text = $"FPS: {fps:F0} | Red: {latenciaMs:F0}ms";
    }
}
