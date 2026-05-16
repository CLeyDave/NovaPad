using System.Windows.Media;

namespace NovaPad.WPF.Overlay.Dibujo;

public static class DisenadorColores
{
    public static Brush CrearFondo(string hex)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(Color.FromArgb(180, c.R, c.G, c.B));
        }
        catch { return new SolidColorBrush(Color.FromArgb(180, 0x11, 0x11, 0x11)); }
    }

    public static Brush CrearAcento(string hex)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(c);
        }
        catch { return new SolidColorBrush(Color.FromRgb(0x00, 0xBC, 0xD4)); }
    }

    public static Brush CrearTexto() =>
        new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));

    public static Brush CrearTextoSecundario() =>
        new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));

    public static Brush CrearBorde() =>
        new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));

    public static Brush NivelBateria(int nivel)
    {
        if (nivel < 0) return CrearTextoSecundario();
        if (nivel <= 15) return new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
        if (nivel <= 30) return new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
        return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
    }

    public static Brush EstadoConexionBrush(bool conectado) =>
        conectado
            ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
            : new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61));
}
