using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NovaPad.WPF.Overlay.Partes;

namespace NovaPad.WPF.Overlay;

public class VentanaPanel : Window
{
    private readonly PanelExtendido _panel;

    public PanelExtendido Panel => _panel;

    public VentanaPanel(string hexAcento)
    {
        Title = "NovaPad Panel";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Colors.Transparent);
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _panel = new PanelExtendido(hexAcento);
        Content = _panel.Vista;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            _panel.Ocultar();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    public void Alternar()
    {
        if (IsVisible)
        {
            Hide();
            _panel.Ocultar();
        }
        else
        {
            _panel.Mostrar();
            Show();
            Activate();
        }
    }
}
