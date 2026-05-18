using System.Windows;
using System.Windows.Controls;

namespace NovaPad.WPF.Overlay.Partes;

public class OrganizadorBurbujas
{
    private readonly List<BurbujaInfo> _grupo = new();
    private readonly StackPanel _panel;
    private readonly Queue<(string T, string C, double D)> _cola = new();
    private int _contadorActivas;
    private const int MaxActivas = 4;
    private const int TamanoGrupo = 8;

    public EstiloAviso Estilo { get; set; } = EstiloAviso.Clasica;

    public OrganizadorBurbujas(StackPanel panel, string acentoHex)
    {
        _panel = panel;
        for (int i = 0; i < TamanoGrupo; i++)
        {
            var b = new BurbujaInfo(acentoHex, Estilo);
            _grupo.Add(b);
            b.Vista.Visibility = System.Windows.Visibility.Collapsed;
            panel.Children.Add(b.Vista);
        }
    }

    public void Lanzar(string titulo, string cuerpo, double duracion)
    {
        if (_contadorActivas >= MaxActivas)
        {
            _cola.Enqueue((titulo, cuerpo, duracion));
            return;
        }

        var burbuja = _grupo.FirstOrDefault(b => !b.Ocupada);
        if (burbuja == null) return;

        _contadorActivas++;
        burbuja.Vista.Visibility = System.Windows.Visibility.Visible;
        burbuja.Mostrar(titulo, cuerpo, duracion);

        _ = DescartarAsync(burbuja, (int)(duracion * 1000)).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                System.Diagnostics.Debug.WriteLine($"[OrganizadorBurbujas] Error in DescartarAsync: {t.Exception?.InnerException?.Message}");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task DescartarAsync(BurbujaInfo b, int ms)
    {
        await Task.Delay(ms);
        await _panel.Dispatcher.InvokeAsync(() => b.Ocultar());
        await Task.Delay(400);
        await _panel.Dispatcher.InvokeAsync(() =>
        {
            b.Vista.Visibility = Visibility.Collapsed;
            _contadorActivas--;
            ProcesarCola();
        });
    }

    private void ProcesarCola()
    {
        if (_cola.Count > 0 && _contadorActivas < MaxActivas)
        {
            var (t, c, d) = _cola.Dequeue();
            Lanzar(t, c, d);
        }
    }

    public void CambiarAcento(string hex)
    {
        foreach (var b in _grupo)
            b.CambiarAcento(hex);
    }

    public void CambiarEstilo(EstiloAviso nuevo, string acentoHex)
    {
        Estilo = nuevo;
        foreach (var b in _grupo)
        {
            _panel.Children.Remove(b.Vista);
        }
        _grupo.Clear();
        _cola.Clear();
        _contadorActivas = 0;
        for (int i = 0; i < TamanoGrupo; i++)
        {
            var b = new BurbujaInfo(acentoHex, nuevo);
            _grupo.Add(b);
            b.Vista.Visibility = System.Windows.Visibility.Collapsed;
            _panel.Children.Add(b.Vista);
        }
    }
}

