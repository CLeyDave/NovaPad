using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NovaPad.Core.Events;
using NovaPad.Core.Interfaces;
using NovaPad.WPF.Overlay.Dibujo;
using NovaPad.WPF.Overlay.Partes;
using Serilog;

namespace NovaPad.WPF.Overlay;

public class VentanaFondo : Window, IOverlayService
{
    private PanelExtendido _panelEx = null!;
    private CuadroDepuracion _depu = null!;
    private OrganizadorBurbujas? _avisos;
    private readonly Dictionary<string, TarjetaMando> _tarjetasPorId = new();
    private readonly StackPanel _zonaAvisos = new();
    private readonly DispatcherTimer _tickDepu = new();
    private InformeMandos? _ultimoInforme;
    private readonly Dictionary<string, OverlayCardConfig> _configPorMando = new();
    private int _frames;
    private DateTime _ultimoTick = DateTime.UtcNow;
    private string _acentoHex = "#00BCD4";
    private double _escala = 1.0;
    private string _anclajeHud = "TopRight";
    private double _desvioX = 20;
    private double _desvioY = 20;
    private string _anclajeAvisos = "BottomRight";
    private double _desvioAvisoX = 20;
    private double _desvioAvisoY = 10;
    private EstiloTarjeta _estiloTarjeta = EstiloTarjeta.Clasico;
    private EstiloAviso _estiloAviso = EstiloAviso.Clasica;
    private bool _verBateria = true;
    private bool _verLatencia = true;
    private bool _verFrecuencia;
    private bool _verPerfil = true;
    private bool _verConexion = true;
    private bool _verFps;
    private bool _verReloj = true;
    private bool _verTipo = true;
    private bool _verAvisos = true;
    private double _duracionAviso = 3.0;

    private const int WM_HOTKEY = 0x0312;
    private const int IdHotkey = 9001;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    private const int VK_O = 0x4F;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const uint LWA_ALPHA = 0x00000002;

    private enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public uint AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    private enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    public VentanaFondo()
    {
        Title = "NovaPad Overlay";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Left = 0;
        Top = 0;

        Content = new Canvas();
    }

    public new bool IsActive => IsVisible;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        InicializarUI();
        Log.Information("[VentanaFondo] UI inicializada");
    }

    private Canvas Lienzo => (Content as Canvas)!;

    private void InicializarUI()
    {
        _depu = new CuadroDepuracion();
        _panelEx = new PanelExtendido(_acentoHex);
        _avisos = new OrganizadorBurbujas(_zonaAvisos, _acentoHex);
        _avisos.CambiarEstilo(_estiloAviso, _acentoHex);

        RecalcularAnclajeAvisos();
        Canvas.SetLeft(_panelEx.Vista, (Width - 360) / 2);
        Canvas.SetTop(_panelEx.Vista, (Height - 400) / 2);
        Canvas.SetLeft(_depu.Vista, 0);
        Canvas.SetTop(_depu.Vista, 0);

        var lienzo = Lienzo;
        lienzo.Children.Add(_zonaAvisos);
        lienzo.Children.Add(_panelEx.Vista);
        lienzo.Children.Add(_depu.Vista);

        _tickDepu.Interval = TimeSpan.FromSeconds(1);
        _tickDepu.Tick += (_, _) =>
        {
            var now = DateTime.UtcNow;
            var segs = (now - _ultimoTick).TotalSeconds;
            _depu.Marcar(segs > 0 ? _frames / segs : 0, 0);
            _frames = 0;
            _ultimoTick = now;
        };
        _tickDepu.Start();

        CompositionTarget.Rendering += (_, _) => _frames++;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);

        RegisterHotKey(hwnd, IdHotkey, MOD_CONTROL | MOD_SHIFT, VK_O);
        Log.Information("[VentanaFondo] Hotkey Ctrl+Shift+O registrado");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            AlternarPanel();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void AlternarPanel()
    {
        if (_panelEx == null) return;
        var abierto = _panelEx.Alternar();
        if (abierto)
        {
            Canvas.SetLeft(_panelEx.Vista, (Width - 360) / 2);
            Canvas.SetTop(_panelEx.Vista, (Height - 400) / 2);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        UnregisterHotKey(helper.Handle, IdHotkey);
        _panelEx.Detener();
        _tickDepu.Stop();
        Log.Information("[VentanaFondo] Ventana cerrada");
        base.OnClosed(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _panelEx != null && _panelEx.Vista.Visibility == Visibility.Visible)
        {
            AlternarPanel();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    public void ApplyConfig(DatosConfigOverlay cfg)
    {
        Opacity = cfg.Opacidad;
        _escala = cfg.Escala;
        _acentoHex = cfg.ColorAcento;
        _duracionAviso = cfg.DuracionAviso;
        _anclajeHud = cfg.AnclajeHud;
        _desvioX = cfg.DesvioX;
        _desvioY = cfg.DesvioY;
        _verBateria = cfg.VerBateria;
        _verLatencia = cfg.VerLatencia;
        _verFrecuencia = cfg.VerFrecuencia;
        _verPerfil = cfg.VerPerfil;
        _verConexion = cfg.VerConexion;
        _verFps = cfg.VerFps;
        _verReloj = cfg.VerReloj;
        _verTipo = cfg.VerTipo;
        _verAvisos = cfg.VerAvisos;

        _depu.Vista.Visibility = _verFps ? Visibility.Visible : Visibility.Collapsed;

        _anclajeAvisos = cfg.AnclajeAvisos;
        _desvioAvisoX = cfg.DesvioAvisoX;
        _desvioAvisoY = cfg.DesvioAvisoY;
        RecalcularAnclajeAvisos();
        _avisos?.CambiarAcento(_acentoHex);

        if (Enum.TryParse<EstiloAviso>(cfg.EstiloAviso, true, out var parsedEstiloAviso) && parsedEstiloAviso != _estiloAviso)
        {
            _estiloAviso = parsedEstiloAviso;
            _avisos?.CambiarEstilo(_estiloAviso, _acentoHex);
        }

        _panelEx.CambiarAcento(_acentoHex, cfg.ColorFondo);

        if (Enum.TryParse<EstiloTarjeta>(cfg.EstiloTarjeta, true, out var parsedEstilo))
        {
            if (parsedEstilo != _estiloTarjeta)
            {
                _estiloTarjeta = parsedEstilo;
                LimpiarTarjetas();
                if (_ultimoInforme != null)
                    ActualizarHud(_ultimoInforme);
            }
            else
            {
                _estiloTarjeta = parsedEstilo;
            }
        }

        foreach (var kv in _tarjetasPorId)
        {
            var t = kv.Value;
            t.Reconfigurar(_acentoHex, cfg.ColorFondo, _escala,
                _verBateria, _verLatencia, _verFrecuencia, _verPerfil, _verConexion, _verTipo);
        }

        ReposicionarTarjetas();
    }

    public void ApplyCardConfig(OverlayCardConfig cfg)
    {
        Log.Information("[Manejador] CardConfig para {Id}", cfg.ControllerId);
        _configPorMando[cfg.ControllerId] = cfg;
        if (_tarjetasPorId.TryGetValue(cfg.ControllerId, out var tarjeta))
        {
            var estiloActual = tarjeta.ObtenerEstilo;
            if (Enum.TryParse<EstiloTarjeta>(cfg.EstiloTarjeta, true, out var nuevoEstilo) && nuevoEstilo != estiloActual)
            {
                ReemplazarTarjeta(cfg.ControllerId, tarjeta, cfg, nuevoEstilo);
            }
            else
            {
                tarjeta.Reconfigurar(cfg, _escala);
            }
        }
        ReposicionarTarjetas();
    }

    public void UpdateHud(InformeMandos inf)
    {
        _ultimoInforme = inf;
        ActualizarHud(inf);
    }

    public void ShowNotification(string title, string message)
    {
        Log.Information("[Manejador] Notificacion: {Titulo} {Msj}", title, message);
        if (_verAvisos)
            _avisos?.Lanzar(title, message, _duracionAviso);
    }

    public void NotifyConnection(EstadoConexion ev)
    {
        Log.Information("[Manejador] Conexion: {Id} {Conectado}", ev.IdMando, ev.Conectado);
        if (_verAvisos)
        {
            var titulo = ev.Conectado ? "Mando Conectado" : "Mando Desconectado";
            _avisos?.Lanzar(titulo, ev.NombreMando, _duracionAviso);
        }
    }

    public void NotifyTheme(CambioTema ev)
    {
        Log.Information("[Manejador] Tema: oscuro={Oscuro}", ev.Oscuro);
    }

    public void TogglePanel()
    {
        Dispatcher.Invoke(AlternarPanel);
    }

    private void LimpiarTarjetas()
    {
        foreach (var t in _tarjetasPorId.Values)
            Lienzo.Children.Remove(t.Vista);
        _tarjetasPorId.Clear();
    }

    private void ActualizarHud(InformeMandos inf)
    {
        foreach (var m in inf.Lista)
        {
            if (!_tarjetasPorId.TryGetValue(m.Id, out var tarjeta))
            {
                tarjeta = CrearTarjeta(m.Id);
                Lienzo.Children.Add(tarjeta.Vista);
                _tarjetasPorId[m.Id] = tarjeta;
                if (_configPorMando.TryGetValue(m.Id, out var cardCfg))
                    tarjeta.Reconfigurar(cardCfg, _escala);
            }
            tarjeta.Aplicar(m);
        }

        var idsActuales = inf.Lista.Select(m => m.Id).ToHashSet();
        var idsRemover = _tarjetasPorId.Keys.Where(id => !idsActuales.Contains(id)).ToList();
        foreach (var id in idsRemover)
        {
            var t = _tarjetasPorId[id];
            Lienzo.Children.Remove(t.Vista);
            _tarjetasPorId.Remove(id);
        }

        ReposicionarTarjetas();
        if (_panelEx != null)
            _panelEx.Actualizar(inf.Lista);
    }

    private void RecalcularAnclajeAvisos()
    {
        Canvas.SetRight(_zonaAvisos, _desvioAvisoX);
        Canvas.SetBottom(_zonaAvisos, _desvioAvisoY);
        Canvas.SetLeft(_zonaAvisos, double.NaN);
        Canvas.SetTop(_zonaAvisos, double.NaN);
    }

    private void ReposicionarTarjetas()
    {
        var items = new List<(TarjetaMando card, string anclaje, double dx, double dy)>();

        foreach (var kv in _tarjetasPorId)
        {
            var card = kv.Value;
            string anclaje; double dx, dy;
            if (_configPorMando.TryGetValue(kv.Key, out var cfg))
            {
                anclaje = cfg.AnclajeHud ?? _anclajeHud;
                dx = cfg.DesvioX ?? _desvioX;
                dy = cfg.DesvioY ?? _desvioY;
            }
            else
            {
                anclaje = _anclajeHud;
                dx = _desvioX;
                dy = _desvioY;
            }
            items.Add((card, anclaje, dx, dy));
        }

        var porPosicion = items.GroupBy(g => (g.anclaje, Math.Round(g.dx, 1), Math.Round(g.dy, 1)));

        foreach (var grupo in porPosicion)
        {
            double yOffset = 0;
            foreach (var (card, anclaje, dx, dy) in grupo)
            {
                var el = card.Vista;
                el.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                el.Arrange(new Rect(new Point(0, 0), el.DesiredSize));
                PosicionarElemento(el, anclaje, dx, dy + yOffset);
                yOffset += el.DesiredSize.Height + 6;
            }
        }

        Lienzo.InvalidateMeasure();
    }

    private void PosicionarElemento(UIElement el, string anclaje, double dx, double dy)
    {
        var fe = (FrameworkElement)el;
        switch (anclaje)
        {
            case "TopLeft":
                Canvas.SetLeft(el, dx); Canvas.SetTop(el, dy);
                Canvas.SetRight(el, double.NaN); Canvas.SetBottom(el, double.NaN);
                break;
            case "TopRight":
                Canvas.SetRight(el, dx); Canvas.SetTop(el, dy);
                Canvas.SetLeft(el, double.NaN); Canvas.SetBottom(el, double.NaN);
                break;
            case "TopCenter":
                Canvas.SetLeft(el, (fe.Parent as FrameworkElement)?.ActualWidth / 2 + dx ?? dx); Canvas.SetTop(el, dy);
                Canvas.SetRight(el, double.NaN); Canvas.SetBottom(el, double.NaN);
                break;
            case "CenterLeft":
                Canvas.SetLeft(el, dx); Canvas.SetTop(el, ((fe.Parent as FrameworkElement)?.ActualHeight ?? Height) / 2 + dy);
                Canvas.SetRight(el, double.NaN); Canvas.SetBottom(el, double.NaN);
                break;
            case "CenterRight":
                Canvas.SetRight(el, dx); Canvas.SetTop(el, ((fe.Parent as FrameworkElement)?.ActualHeight ?? Height) / 2 + dy);
                Canvas.SetLeft(el, double.NaN); Canvas.SetBottom(el, double.NaN);
                break;
            case "Center":
                Canvas.SetLeft(el, ((fe.Parent as FrameworkElement)?.ActualWidth ?? Width) / 2 + dx);
                Canvas.SetTop(el, ((fe.Parent as FrameworkElement)?.ActualHeight ?? Height) / 2 + dy);
                Canvas.SetRight(el, double.NaN); Canvas.SetBottom(el, double.NaN);
                break;
            case "BottomLeft":
                Canvas.SetLeft(el, dx); Canvas.SetBottom(el, dy);
                Canvas.SetRight(el, double.NaN); Canvas.SetTop(el, double.NaN);
                break;
            case "BottomCenter":
                Canvas.SetLeft(el, ((fe.Parent as FrameworkElement)?.ActualWidth ?? Width) / 2 + dx); Canvas.SetBottom(el, dy);
                Canvas.SetRight(el, double.NaN); Canvas.SetTop(el, double.NaN);
                break;
            case "BottomRight":
                Canvas.SetRight(el, dx); Canvas.SetBottom(el, dy);
                Canvas.SetLeft(el, double.NaN); Canvas.SetTop(el, double.NaN);
                break;
            default:
                Canvas.SetRight(el, dx); Canvas.SetTop(el, dy);
                Canvas.SetLeft(el, double.NaN); Canvas.SetBottom(el, double.NaN);
                break;
        }
    }

    private TarjetaMando CrearTarjeta(string id)
    {
        var t = new TarjetaMando(_acentoHex, _estiloTarjeta);
        t.Reconfigurar(_acentoHex, "#222222", _escala,
            _verBateria, _verLatencia, _verFrecuencia, _verPerfil, _verConexion, _verTipo);
        return t;
    }

    private void ReemplazarTarjeta(string id, TarjetaMando vieja, OverlayCardConfig cfg, EstiloTarjeta estilo)
    {
        Lienzo.Children.Remove(vieja.Vista);
        var nueva = new TarjetaMando(cfg.ColorAcento, estilo);
        nueva.Reconfigurar(cfg, _escala);
        _tarjetasPorId[id] = nueva;
        Lienzo.Children.Add(nueva.Vista);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        Log.Information("[VentanaFondo] Cerrando overlay...");
        base.OnClosing(e);
    }
}
