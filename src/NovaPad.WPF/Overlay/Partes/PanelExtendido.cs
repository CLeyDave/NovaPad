using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NovaPad.Core.Events;
using NovaPad.WPF.Overlay.Dibujo;

namespace NovaPad.WPF.Overlay.Partes;

public class PanelExtendido
{
    private readonly Border _marco;
    private readonly StackPanel _lista;
    private readonly TextBlock _mensajeVacio;
    private readonly TextBlock _relojTexto;
    private readonly TextBlock _pcBatteryTexto;
    private readonly DispatcherTimer _batteryTimer;
    private readonly PanelControlMultimedia _mediaControl;
    private readonly DispatcherTimer _temporizador;
    private bool _visible;

    public UIElement Vista => _marco;

    public PanelExtendido(string hexAcento)
    {
        _relojTexto = new TextBlock
        {
            FontSize = 48,
            FontWeight = FontWeights.Light,
            Foreground = DisenadorColores.CrearAcento(hexAcento),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };

        _temporizador = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _temporizador.Tick += (_, _) => RefrescarReloj();
        _temporizador.Start();
        RefrescarReloj();

        _pcBatteryTexto = new TextBlock
        {
            FontSize = 11,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 8)
        };
        _batteryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _batteryTimer.Tick += (_, _) => RefrescarBateriaPC();
        _batteryTimer.Start();
        RefrescarBateriaPC();

        _mediaControl = new PanelControlMultimedia(hexAcento);

        _mensajeVacio = new TextBlock
        {
            Text = "Sin mandos",
            FontSize = 14,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };

        _lista = new StackPanel();
        _lista.Children.Add(_mensajeVacio);

        var linea = new Border
        {
            Height = 1,
            Background = DisenadorColores.CrearBorde(),
            Margin = new Thickness(0, 8, 0, 12)
        };

        var lineaMedia = new Border
        {
            Height = 1,
            Background = DisenadorColores.CrearBorde(),
            Margin = new Thickness(0, 8, 0, 12)
        };

        var titulo = new TextBlock
        {
            Text = "Panel de Control",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = DisenadorColores.CrearTexto(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var pila = new StackPanel();
        pila.Children.Add(_relojTexto);
        pila.Children.Add(_pcBatteryTexto);
        pila.Children.Add(linea);
        pila.Children.Add(_mediaControl.Vista);
        pila.Children.Add(lineaMedia);
        pila.Children.Add(titulo);
        pila.Children.Add(_lista);

        _marco = new Border
        {
            Child = pila,
            Background = DisenadorColores.CrearFondo("#1A1A2E"),
            BorderBrush = DisenadorColores.CrearBorde(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(28, 20, 28, 20),
            MinWidth = 320,
            MaxWidth = 420,
            Opacity = 0,
            Visibility = Visibility.Collapsed,
            RenderTransform = new ScaleTransform(0.9, 0.9),
            RenderTransformOrigin = new Point(0.5, 0.5),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 6,
                Color = Colors.Black,
                Opacity = 0.5
            }
        };
    }

    private void RefrescarReloj()
    {
        _relojTexto.Text = DateTime.Now.ToString("h:mm:ss");
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte Reserved1;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    private void RefrescarBateriaPC()
    {
        if (GetSystemPowerStatus(out var sps))
        {
            var nivel = sps.BatteryLifePercent;
            if (nivel > 100) nivel = 100;
            var cargando = sps.ACLineStatus == 1;
            var icono = cargando ? "\u26A1" : (nivel <= 20 ? "\uD83E\uDEAB" : "\uD83D\uDD0B");
            _pcBatteryTexto.Text = $"{icono} PC: {nivel}%{(cargando ? " (cargando)" : "")}";
        }
    }

    public bool Alternar()
    {
        _visible = !_visible;
        if (_visible)
            Mostrar();
        else
            Ocultar();
        return _visible;
    }

    public void Mostrar()
    {
        _marco.Visibility = Visibility.Visible;
        _marco.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
        _marco.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(250)));
        _marco.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(250)));
    }

    public void Ocultar()
    {
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        anim.Completed += (_, _) => _marco.Visibility = Visibility.Collapsed;
        _marco.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    public void CambiarAcento(string hexAcento, string hexFondo)
    {
        _marco.Background = DisenadorColores.CrearFondo(hexFondo);
        _relojTexto.Foreground = DisenadorColores.CrearAcento(hexAcento);
        _mediaControl.CambiarAcento(hexAcento);
    }

    public void Actualizar(List<InfoMando> mandos)
    {
        _lista.Children.Clear();

        if (mandos.Count == 0)
        {
            _lista.Children.Add(_mensajeVacio);
            return;
        }

        foreach (var m in mandos)
        {
            if (string.IsNullOrWhiteSpace(m.Nombre)) continue;
            var punto = new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = DisenadorColores.EstadoConexionBrush(m.Conectado),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var nombre = new TextBlock
            {
                Text = m.Nombre,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = DisenadorColores.CrearTexto()
            };

            var detalles = new TextBlock
            {
                FontSize = 11,
                Foreground = DisenadorColores.CrearTextoSecundario(),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var partes = new List<string>();
            if (m.NivelBateria >= 0)
                partes.Add($"Bat: {m.NivelBateria}%{(m.Cargando ? " ⚡" : "")}");
            if (m.LatenciaMs > 0)
                partes.Add($"Lat: {m.LatenciaMs:F1}ms");
            if (m.Hz > 0)
                partes.Add($"{m.Hz}Hz");
            if (!string.IsNullOrEmpty(m.PerfilActivo))
                partes.Add($"Perfil: {m.PerfilActivo}");
            if (!string.IsNullOrEmpty(m.TipoMando))
                partes.Add(m.TipoMando);

            detalles.Text = partes.Count > 0 ? string.Join(" · ", partes) : "";

            var columna = new StackPanel();
            columna.Children.Add(nombre);
            columna.Children.Add(detalles);

            var fila = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            fila.Children.Add(punto);
            fila.Children.Add(columna);

            _lista.Children.Add(fila);
        }
    }

    public void Detener()
    {
        _temporizador.Stop();
        _batteryTimer.Stop();
        _mediaControl.Detener();
    }
}
