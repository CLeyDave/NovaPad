using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NovaPad.WPF.Overlay.Dibujo;

namespace NovaPad.WPF.Overlay.Partes;

public class MarcadorHora
{
    private readonly TextBlock _texto;
    private readonly DispatcherTimer _temporizador;

    public UIElement Vista => _texto;

    public MarcadorHora()
    {
        _texto = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Foreground = DisenadorColores.CrearTexto(),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 10, 16, 0)
        };

        _temporizador = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _temporizador.Tick += (_, _) => Refrescar();
        _temporizador.Start();

        Refrescar();
    }

    private void Refrescar()
    {
        _texto.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    public void CambiarColor(string hex)
    {
        _texto.Foreground = DisenadorColores.CrearAcento(hex);
    }

    public void Detener()
    {
        _temporizador.Stop();
    }
}
