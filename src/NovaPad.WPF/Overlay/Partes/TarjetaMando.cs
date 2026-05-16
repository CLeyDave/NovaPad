using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovaPad.Core.Events;
using NovaPad.WPF.Overlay.Dibujo;

namespace NovaPad.WPF.Overlay.Partes;

public enum EstiloTarjeta
{
    Clasico,
    Moderno,
    Mini,
    Terminal,
    Minimal,
    SoloBateria,
    CompactoVertical,
    InputVisualizer,
    Linea,
    Detallado
}

public class TarjetaMando
{
    private readonly Border _cajon;
    private readonly EstiloTarjeta _estilo;
    public EstiloTarjeta ObtenerEstilo => _estilo;
    private readonly StackPanel _contenedor;

    private Border? _avatar;
    private TextBlock? _avatarLetras;
    private Border? _puntoConexion;
    private TextBlock? _titulo;
    private TextBlock? _detalles;
    private Border? _barraTrack;
    private Border? _barraNivel;
    private TextBlock? _textoBateria;
    private Canvas? _stickIzq, _stickDer;
    private Border? _dotIzq, _dotDer;
    private Border? _trkL2, _nvlL2, _trkR2, _nvlR2;
    private TextBlock? _txtL2, _txtR2;
    private Border? _btnA, _btnB, _btnX, _btnY;
    private TextBlock? _textoLatencia, _textoPorcentaje;

    private string _bgHex = "#222222";
    private double _escala = 1.0;
    private bool _verBateria = true;
    private bool _verLatencia = true;
    private bool _verFrecuencia;
    private bool _verPerfil = true;
    private bool _verConexion = true;
    private bool _verTipo = true;
    private string _acentoHex = "#00BCD4";

    public UIElement Vista => _cajon;

    public TarjetaMando(string hexAcento, EstiloTarjeta estilo = EstiloTarjeta.Moderno)
    {
        _acentoHex = hexAcento;
        _estilo = estilo;
        _contenedor = new StackPanel();

        switch (estilo)
        {
            case EstiloTarjeta.Clasico: ConstruirClasico(); break;
            case EstiloTarjeta.Moderno: ConstruirModerno(); break;
            case EstiloTarjeta.Mini: ConstruirMini(); break;
            case EstiloTarjeta.Terminal: ConstruirTerminal(); break;
            case EstiloTarjeta.Minimal: ConstruirMinimal(); break;
            case EstiloTarjeta.SoloBateria: ConstruirSoloBateria(); break;
            case EstiloTarjeta.CompactoVertical: ConstruirCompactoVertical(); break;
            case EstiloTarjeta.InputVisualizer: ConstruirInputVisualizer(); break;
            case EstiloTarjeta.Linea: ConstruirLinea(); break;
            case EstiloTarjeta.Detallado: ConstruirDetallado(); break;
        }

        _cajon = new Border
        {
            Child = _contenedor,
            Background = DisenadorColores.CrearFondo(_bgHex),
            BorderBrush = DisenadorColores.CrearBorde(),
            BorderThickness = new Thickness(estilo == EstiloTarjeta.Minimal ? 0 : 1),
            CornerRadius = new CornerRadius(estilo == EstiloTarjeta.Terminal ? 4 : 10),
            Padding = ObtenerPadding(),
            Margin = new Thickness(0, 0, 0, 6),
            MinWidth = ObtenerMinWidth(),
            MaxWidth = ObtenerMaxWidth()
        };

        AplicarEstiloVisual();
    }

    private void AplicarEstiloVisual()
    {
    }

    private Thickness ObtenerPadding() => _estilo switch
    {
        EstiloTarjeta.Minimal => new Thickness(4),
        EstiloTarjeta.SoloBateria => new Thickness(8, 4, 8, 4),
        EstiloTarjeta.Terminal => new Thickness(10, 6, 10, 6),
        EstiloTarjeta.InputVisualizer => new Thickness(10, 6, 10, 6),
        _ => new Thickness(12, 8, 12, 8)
    };

    private double ObtenerMinWidth() => _estilo switch
    {
        EstiloTarjeta.Minimal => 60,
        EstiloTarjeta.SoloBateria => 90,
        EstiloTarjeta.CompactoVertical => 120,
        EstiloTarjeta.InputVisualizer => 180,
        EstiloTarjeta.Mini => 130,
        _ => 220
    };

    private double ObtenerMaxWidth() => _estilo switch
    {
        EstiloTarjeta.Minimal => 120,
        EstiloTarjeta.SoloBateria => 150,
        EstiloTarjeta.InputVisualizer => 250,
        _ => 320
    };

    private void ConstruirClasico()
    {
        _titulo = new TextBlock
        {
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = DisenadorColores.CrearTexto()
        };

        _puntoConexion = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = DisenadorColores.EstadoConexionBrush(true),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var filaTitulo = new StackPanel { Orientation = Orientation.Horizontal };
        filaTitulo.Children.Add(_puntoConexion);
        filaTitulo.Children.Add(_titulo);

        _detalles = new TextBlock
        {
            FontSize = 11,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(16, 2, 0, 0)
        };

        _contenedor.Children.Add(filaTitulo);
        _contenedor.Children.Add(_detalles);
    }

    private void ConstruirModerno()
    {
        _avatarLetras = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _avatar = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = DisenadorColores.CrearAcento(_acentoHex),
            Child = _avatarLetras,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 12, 0)
        };

        _titulo = new TextBlock
        {
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = DisenadorColores.CrearTexto(),
            VerticalAlignment = VerticalAlignment.Center
        };

        _puntoConexion = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = DisenadorColores.EstadoConexionBrush(true),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };

        var filaTitulo = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        filaTitulo.Children.Add(_puntoConexion);
        filaTitulo.Children.Add(_titulo);

        _barraNivel = new Border
        {
            Height = 6,
            CornerRadius = new CornerRadius(3),
            Background = DisenadorColores.NivelBateria(100),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0
        };

        _barraTrack = new Border
        {
            Height = 6,
            CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0xFF)),
            MinWidth = 60,
            Child = _barraNivel
        };

        _textoBateria = new TextBlock
        {
            FontSize = 11,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var filaBateria = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 2) };
        filaBateria.Children.Add(_barraTrack);
        filaBateria.Children.Add(_textoBateria);

        _detalles = new TextBlock
        {
            FontSize = 11,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var columna = new StackPanel();
        columna.Children.Add(filaTitulo);
        columna.Children.Add(filaBateria);
        columna.Children.Add(_detalles);

        var filaPrincipal = new StackPanel { Orientation = Orientation.Horizontal };
        filaPrincipal.Children.Add(_avatar);
        filaPrincipal.Children.Add(columna);

        _contenedor.Children.Add(filaPrincipal);
    }

    private void ConstruirMini()
    {
        _avatarLetras = new TextBlock
        {
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _avatar = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(15),
            Background = DisenadorColores.CrearAcento(_acentoHex),
            Child = _avatarLetras,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };

        _barraNivel = new Border
        {
            Height = 5,
            CornerRadius = new CornerRadius(2.5),
            Background = DisenadorColores.NivelBateria(100),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0
        };

        _barraTrack = new Border
        {
            Height = 5,
            CornerRadius = new CornerRadius(2.5),
            Background = new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0xFF)),
            MinWidth = 50,
            Child = _barraNivel
        };

        _textoBateria = new TextBlock
        {
            FontSize = 10,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var fila = new StackPanel { Orientation = Orientation.Horizontal };
        fila.Children.Add(_avatar);
        fila.Children.Add(_barraTrack);
        fila.Children.Add(_textoBateria);

        _contenedor.Children.Add(fila);
    }

    private void ConstruirTerminal()
    {
        _titulo = new TextBlock
        {
            FontSize = 12,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x00)),
            Margin = new Thickness(0, 0, 0, 2)
        };

        _detalles = new TextBlock
        {
            FontSize = 11,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0x66)),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        _contenedor.Children.Add(_titulo);
        _contenedor.Children.Add(_detalles);
        _bgHex = "#0A0A0A";
    }

    private void ConstruirMinimal()
    {
        _avatarLetras = new TextBlock
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = DisenadorColores.CrearAcento(_acentoHex),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _textoBateria = new TextBlock
        {
            FontSize = 11,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0)
        };

        var fila = new StackPanel { Orientation = Orientation.Horizontal };
        fila.Children.Add(_avatarLetras);
        fila.Children.Add(_textoBateria);

        _contenedor.Children.Add(fila);
    }

    private void ConstruirSoloBateria()
    {
        _barraNivel = new Border
        {
            Height = 18,
            CornerRadius = new CornerRadius(9),
            Background = DisenadorColores.NivelBateria(100),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0
        };

        _barraTrack = new Border
        {
            Height = 18,
            CornerRadius = new CornerRadius(9),
            Background = new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0xFF)),
            MinWidth = 100,
            Child = _barraNivel
        };

        _textoBateria = new TextBlock
        {
            FontSize = 24,
            FontWeight = FontWeights.Light,
            Foreground = DisenadorColores.CrearTexto(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        };

        _contenedor.Children.Add(_barraTrack);
        _contenedor.Children.Add(_textoBateria);
    }

    private void ConstruirCompactoVertical()
    {
        _titulo = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = DisenadorColores.CrearTexto(),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        _puntoConexion = new Border
        {
            Width = 6,
            Height = 6,
            CornerRadius = new CornerRadius(3),
            Background = DisenadorColores.EstadoConexionBrush(true),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };

        _barraNivel = new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = DisenadorColores.NivelBateria(100),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0,
            VerticalAlignment = VerticalAlignment.Center
        };

        _barraTrack = new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0xFF)),
            MinWidth = 40,
            Child = _barraNivel,
            VerticalAlignment = VerticalAlignment.Center
        };

        _textoBateria = new TextBlock
        {
            FontSize = 10,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var fila = new StackPanel { Orientation = Orientation.Horizontal };
        fila.Children.Add(_puntoConexion);
        fila.Children.Add(_titulo);
        fila.Children.Add(_barraTrack);
        fila.Children.Add(_textoBateria);

        _contenedor.Children.Add(fila);
    }

    private void ConstruirInputVisualizer()
    {
        _titulo = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = DisenadorColores.CrearTexto(),
            VerticalAlignment = VerticalAlignment.Center
        };

        _puntoConexion = new Border
        {
            Width = 6, Height = 6, CornerRadius = new CornerRadius(3),
            Background = DisenadorColores.EstadoConexionBrush(true),
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0)
        };

        _textoBateria = new TextBlock
        {
            FontSize = 10, Foreground = DisenadorColores.CrearTextoSecundario(),
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right
        };

        var filaTop = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        filaTop.ColumnDefinitions.Add(new ColumnDefinition());
        filaTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var panelTitulo = new StackPanel { Orientation = Orientation.Horizontal };
        panelTitulo.Children.Add(_puntoConexion);
        panelTitulo.Children.Add(_titulo);
        filaTop.Children.Add(panelTitulo);
        Grid.SetColumn(_textoBateria, 1);
        filaTop.Children.Add(_textoBateria);

        _dotIzq = new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = DisenadorColores.CrearAcento(_acentoHex) };
        _stickIzq = new Canvas { Width = 50, Height = 50 };
        _stickIzq.Background = new SolidColorBrush(Color.FromArgb(30, 0xFF, 0xFF, 0xFF));
        _stickIzq.Children.Add(_dotIzq);

        _dotDer = new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = DisenadorColores.CrearAcento(_acentoHex) };
        _stickDer = new Canvas { Width = 50, Height = 50 };
        _stickDer.Background = new SolidColorBrush(Color.FromArgb(30, 0xFF, 0xFF, 0xFF));
        _stickDer.Children.Add(_dotDer);

        var filaSticks = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) };
        filaSticks.Children.Add(_stickIzq);
        filaSticks.Children.Add(new TextBlock { Text = "  ", FontSize = 10 });
        filaSticks.Children.Add(_stickDer);

        _nvlL2 = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = DisenadorColores.CrearAcento(_acentoHex), HorizontalAlignment = HorizontalAlignment.Left, Width = 0 };
        _trkL2 = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0xFF)), MinWidth = 60, Child = _nvlL2 };
        _txtL2 = new TextBlock { FontSize = 9, Foreground = DisenadorColores.CrearTextoSecundario(), Text = "L2", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };

        _nvlR2 = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = DisenadorColores.CrearAcento(_acentoHex), HorizontalAlignment = HorizontalAlignment.Left, Width = 0 };
        _trkR2 = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0xFF)), MinWidth = 60, Child = _nvlR2 };
        _txtR2 = new TextBlock { FontSize = 9, Foreground = DisenadorColores.CrearTextoSecundario(), Text = "R2", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };

        var filaL2 = new StackPanel { Orientation = Orientation.Horizontal };
        filaL2.Children.Add(_trkL2);
        filaL2.Children.Add(_txtL2);
        var filaR2 = new StackPanel { Orientation = Orientation.Horizontal };
        filaR2.Children.Add(_trkR2);
        filaR2.Children.Add(_txtR2);

        var filaTriggers = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) };
        filaTriggers.Children.Add(filaL2);
        filaTriggers.Children.Add(new TextBlock { Text = "  ", FontSize = 8 });
        filaTriggers.Children.Add(filaR2);

        _btnY = CrearBotonVisual("Y");
        _btnB = CrearBotonVisual("B");
        _btnA = CrearBotonVisual("A");
        _btnX = CrearBotonVisual("X");

        var grdBotones = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
        grdBotones.ColumnDefinitions.Add(new ColumnDefinition());
        grdBotones.ColumnDefinitions.Add(new ColumnDefinition());
        grdBotones.ColumnDefinitions.Add(new ColumnDefinition());
        grdBotones.RowDefinitions.Add(new RowDefinition());
        grdBotones.RowDefinitions.Add(new RowDefinition());
        grdBotones.RowDefinitions.Add(new RowDefinition());
        Grid.SetRow(_btnY, 0); Grid.SetColumn(_btnY, 1);
        Grid.SetRow(_btnX, 1); Grid.SetColumn(_btnX, 0);
        Grid.SetRow(_btnB, 1); Grid.SetColumn(_btnB, 2);
        Grid.SetRow(_btnA, 2); Grid.SetColumn(_btnA, 1);
        grdBotones.Children.Add(_btnY);
        grdBotones.Children.Add(_btnX);
        grdBotones.Children.Add(_btnB);
        grdBotones.Children.Add(_btnA);

        _contenedor.Children.Add(filaTop);
        _contenedor.Children.Add(filaSticks);
        _contenedor.Children.Add(filaTriggers);
        _contenedor.Children.Add(grdBotones);
    }

    private static Border CrearBotonVisual(string texto)
    {
        return new Border
        {
            Width = 22, Height = 22, CornerRadius = new CornerRadius(11),
            Background = new SolidColorBrush(Color.FromArgb(40, 0xFF, 0xFF, 0xFF)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(80, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = texto, FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(120, 0xFF, 0xFF, 0xFF)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private void ConstruirLinea()
    {
        _puntoConexion = new Border
        {
            Width = 6, Height = 6, CornerRadius = new CornerRadius(3),
            Background = DisenadorColores.EstadoConexionBrush(true),
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0)
        };

        _titulo = new TextBlock
        {
            FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = DisenadorColores.CrearTexto(),
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0)
        };

        _detalles = new TextBlock
        {
            FontSize = 10, Foreground = DisenadorColores.CrearTextoSecundario(),
            VerticalAlignment = VerticalAlignment.Center
        };

        var fila = new StackPanel { Orientation = Orientation.Horizontal };
        fila.Children.Add(_puntoConexion);
        fila.Children.Add(_titulo);
        fila.Children.Add(_detalles);
        _contenedor.Children.Add(fila);
    }

    private void ConstruirDetallado()
    {
        _avatarLetras = new TextBlock
        {
            FontSize = 16, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _avatar = new Border
        {
            Width = 40, Height = 40, CornerRadius = new CornerRadius(20),
            Background = DisenadorColores.CrearAcento(_acentoHex),
            Child = _avatarLetras, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 12, 0)
        };

        _titulo = new TextBlock
        {
            FontSize = 14, FontWeight = FontWeights.Bold,
            Foreground = DisenadorColores.CrearTexto(),
            VerticalAlignment = VerticalAlignment.Center
        };

        _puntoConexion = new Border
        {
            Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
            Background = DisenadorColores.EstadoConexionBrush(true),
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0)
        };

        var filaTitulo = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        filaTitulo.Children.Add(_puntoConexion);
        filaTitulo.Children.Add(_titulo);

        _barraNivel = new Border
        {
            Height = 8, CornerRadius = new CornerRadius(4),
            Background = DisenadorColores.NivelBateria(100),
            HorizontalAlignment = HorizontalAlignment.Left, Width = 0
        };

        _barraTrack = new Border
        {
            Height = 8, CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0xFF)),
            MinWidth = 60, Child = _barraNivel
        };

        _textoPorcentaje = new TextBlock
        {
            FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = DisenadorColores.CrearTexto(),
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0)
        };

        var filaBateria = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        filaBateria.Children.Add(_barraTrack);
        filaBateria.Children.Add(_textoPorcentaje);

        _detalles = new TextBlock
        {
            FontSize = 10, Foreground = DisenadorColores.CrearTextoSecundario(),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var columna = new StackPanel();
        columna.Children.Add(filaTitulo);
        columna.Children.Add(filaBateria);
        columna.Children.Add(_detalles);

        var filaPrincipal = new StackPanel { Orientation = Orientation.Horizontal };
        filaPrincipal.Children.Add(_avatar);
        filaPrincipal.Children.Add(columna);
        _contenedor.Children.Add(filaPrincipal);
    }

    private void AplicarInputVisualizer(InfoMando datos)
    {
        _titulo!.Text = datos.Nombre;
        _puntoConexion!.Background = DisenadorColores.EstadoConexionBrush(datos.Conectado);

        if (_verBateria && datos.NivelBateria >= 0)
        {
            _textoBateria!.Text = $"{datos.NivelBateria}%{(datos.Cargando ? " ⚡" : "")}";
            _textoBateria.Visibility = Visibility.Visible;
        }
        else
            _textoBateria!.Visibility = Visibility.Collapsed;

        var sw = _stickIzq!.ActualWidth > 0 ? _stickIzq.ActualWidth : 50 * _escala;
        var sh = _stickIzq.ActualHeight > 0 ? _stickIzq.ActualHeight : 50 * _escala;
        var cx = sw / 2; var cy = sh / 2; var radio = (sw / 2 - 7 * _escala);
        Canvas.SetLeft(_dotIzq!, cx + datos.Lx * radio);
        Canvas.SetTop(_dotIzq!, cy - datos.Ly * radio);
        Canvas.SetLeft(_dotDer!, cx + datos.Rx * radio);
        Canvas.SetTop(_dotDer!, cy - datos.Ry * radio);

        var wL2 = datos.L2 * 60 * _escala;
        _nvlL2!.Width = wL2;
        _nvlL2.Background = DisenadorColores.NivelBateria((int)(datos.L2 * 100));
        _nvlL2.Visibility = wL2 > 0 ? Visibility.Visible : Visibility.Collapsed;
        _txtL2!.Text = $"L2 {(int)(datos.L2 * 100)}%";

        var wR2 = datos.R2 * 60 * _escala;
        _nvlR2!.Width = wR2;
        _nvlR2.Background = DisenadorColores.NivelBateria((int)(datos.R2 * 100));
        _nvlR2.Visibility = wR2 > 0 ? Visibility.Visible : Visibility.Collapsed;
        _txtR2!.Text = $"R2 {(int)(datos.R2 * 100)}%";

        AplicarBotonVisual(_btnA!, datos.BtnA);
        AplicarBotonVisual(_btnB!, datos.BtnB);
        AplicarBotonVisual(_btnX!, datos.BtnX);
        AplicarBotonVisual(_btnY!, datos.BtnY);
    }

    private static void AplicarBotonVisual(Border btn, bool presionado)
    {
        var alpha = presionado ? (byte)200 : (byte)40;
        (btn.Child as TextBlock)!.Foreground = new SolidColorBrush(Color.FromArgb(alpha, 0xFF, 0xFF, 0xFF));
        btn.Background = presionado
            ? new SolidColorBrush(Color.FromArgb(120, 0x00, 0xBC, 0xD4))
            : new SolidColorBrush(Color.FromArgb(40, 0xFF, 0xFF, 0xFF));
    }

    private static string ObtenerIniciales(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "?";
        var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1)
            return partes[0][..1].ToUpper();
        return partes[0][..1].ToUpper() + partes[^1][..1].ToUpper();
    }

    public void Aplicar(InfoMando datos)
    {
        switch (_estilo)
        {
            case EstiloTarjeta.Clasico: AplicarClasico(datos); break;
            case EstiloTarjeta.Moderno: AplicarModerno(datos); break;
            case EstiloTarjeta.Mini: AplicarMini(datos); break;
            case EstiloTarjeta.Terminal: AplicarTerminal(datos); break;
            case EstiloTarjeta.Minimal: AplicarMinimal(datos); break;
            case EstiloTarjeta.SoloBateria: AplicarSoloBateria(datos); break;
            case EstiloTarjeta.CompactoVertical: AplicarCompactoVertical(datos); break;
            case EstiloTarjeta.InputVisualizer: AplicarInputVisualizer(datos); break;
            case EstiloTarjeta.Linea: AplicarLinea(datos); break;
            case EstiloTarjeta.Detallado: AplicarDetallado(datos); break;
        }
    }

    private void AplicarClasico(InfoMando datos)
    {
        _titulo!.Text = datos.Nombre;
        _puntoConexion!.Background = DisenadorColores.EstadoConexionBrush(datos.Conectado);

        var partes = new List<string>();

        if (_verBateria && datos.NivelBateria >= 0)
            partes.Add($"Bat: {datos.NivelBateria}%{(datos.Cargando ? " ⚡" : "")}");

        if (_verLatencia && datos.LatenciaMs > 0)
            partes.Add($"Lat: {datos.LatenciaMs:F1}ms");

        if (_verFrecuencia && datos.Hz > 0)
            partes.Add($"{datos.Hz}Hz");

        if (_verPerfil && !string.IsNullOrEmpty(datos.PerfilActivo))
            partes.Add($"Perfil: {datos.PerfilActivo}");

        if (_verTipo && !string.IsNullOrEmpty(datos.TipoMando))
            partes.Add(datos.TipoMando);

        _detalles!.Text = partes.Count > 0 ? string.Join(" · ", partes) : "";
    }

    private void AplicarModerno(InfoMando datos)
    {
        _avatarLetras!.Text = ObtenerIniciales(datos.Nombre);
        _titulo!.Text = datos.Nombre;
        _puntoConexion!.Background = DisenadorColores.EstadoConexionBrush(datos.Conectado);

        if (_verBateria && datos.NivelBateria >= 0)
        {
            var ancho = datos.NivelBateria / 100.0 * 60;
            _barraNivel!.Width = ancho;
            _barraNivel.Background = DisenadorColores.NivelBateria(datos.NivelBateria);
            _textoBateria!.Text = $"{datos.NivelBateria}%{(datos.Cargando ? " ⚡" : "")}";
            _barraNivel.Visibility = Visibility.Visible;
            _textoBateria.Visibility = Visibility.Visible;
        }
        else
        {
            _barraNivel!.Visibility = Visibility.Collapsed;
            _textoBateria!.Visibility = Visibility.Collapsed;
        }

        var partes = new List<string>();
        if (_verLatencia && datos.LatenciaMs > 0)
            partes.Add($"Lat: {datos.LatenciaMs:F1}ms");
        if (_verFrecuencia && datos.Hz > 0)
            partes.Add($"{datos.Hz}Hz");
        if (_verPerfil && !string.IsNullOrEmpty(datos.PerfilActivo))
            partes.Add($"Perfil: {datos.PerfilActivo}");
        if (_verTipo && !string.IsNullOrEmpty(datos.TipoMando))
            partes.Add(datos.TipoMando);

        _detalles!.Text = partes.Count > 0 ? string.Join(" · ", partes) : "";
    }

    private void AplicarMini(InfoMando datos)
    {
        _avatarLetras!.Text = ObtenerIniciales(datos.Nombre);

        if (_verBateria && datos.NivelBateria >= 0)
        {
            var ancho = datos.NivelBateria / 100.0 * 50;
            _barraNivel!.Width = ancho;
            _barraNivel.Background = DisenadorColores.NivelBateria(datos.NivelBateria);
            _textoBateria!.Text = $"{datos.NivelBateria}%";
            _barraNivel.Visibility = Visibility.Visible;
            _textoBateria.Visibility = Visibility.Visible;
        }
        else
        {
            _barraNivel!.Visibility = Visibility.Collapsed;
            _textoBateria!.Visibility = Visibility.Collapsed;
        }
    }

    private void AplicarTerminal(InfoMando datos)
    {
        _titulo!.Text = $"> {datos.Nombre} {(datos.Conectado ? "[CONECTADO]" : "[DESCONECTADO]")}";

        var partes = new List<string>();
        if (_verBateria && datos.NivelBateria >= 0)
            partes.Add($"bat:{datos.NivelBateria}%");
        if (_verLatencia && datos.LatenciaMs > 0)
            partes.Add($"lat:{datos.LatenciaMs:F1}ms");
        if (_verFrecuencia && datos.Hz > 0)
            partes.Add($"hz:{datos.Hz}");
        if (_verPerfil && !string.IsNullOrEmpty(datos.PerfilActivo))
            partes.Add($"pf:{datos.PerfilActivo}");

        _detalles!.Text = partes.Count > 0 ? "> " + string.Join(" | ", partes) : "";
    }

    private void AplicarMinimal(InfoMando datos)
    {
        _avatarLetras!.Text = ObtenerIniciales(datos.Nombre);
        _textoBateria!.Text = _verBateria && datos.NivelBateria >= 0
            ? $"{datos.NivelBateria}%" : "";
    }

    private void AplicarSoloBateria(InfoMando datos)
    {
        if (_verBateria && datos.NivelBateria >= 0)
        {
            var trackWidth = _barraTrack!.ActualWidth > 0 ? _barraTrack.ActualWidth : 100;
            var ancho = datos.NivelBateria / 100.0 * trackWidth;
            _barraNivel!.Width = ancho;
            _barraNivel.Background = DisenadorColores.NivelBateria(datos.NivelBateria);
            _textoBateria!.Text = $"{datos.NivelBateria}%";
            _barraNivel.Visibility = Visibility.Visible;
            _textoBateria.Visibility = Visibility.Visible;
        }
        else
        {
            _barraNivel!.Visibility = Visibility.Collapsed;
            _textoBateria!.Visibility = Visibility.Collapsed;
        }
    }

    private void AplicarCompactoVertical(InfoMando datos)
    {
        _titulo!.Text = datos.Nombre;
        _puntoConexion!.Background = DisenadorColores.EstadoConexionBrush(datos.Conectado);

        if (_verBateria && datos.NivelBateria >= 0)
        {
            var ancho = datos.NivelBateria / 100.0 * 40;
            _barraNivel!.Width = ancho;
            _barraNivel.Background = DisenadorColores.NivelBateria(datos.NivelBateria);
            _textoBateria!.Text = $"{datos.NivelBateria}%";
            _barraNivel.Visibility = Visibility.Visible;
            _textoBateria.Visibility = Visibility.Visible;
        }
        else
        {
            _barraNivel!.Visibility = Visibility.Collapsed;
            _textoBateria!.Visibility = Visibility.Collapsed;
        }
    }

    private void AplicarLinea(InfoMando datos)
    {
        _puntoConexion!.Background = DisenadorColores.EstadoConexionBrush(datos.Conectado);
        _titulo!.Text = datos.Nombre;

        var partes = new List<string>();
        if (_verBateria && datos.NivelBateria >= 0)
            partes.Add($"{datos.NivelBateria}%");
        if (_verLatencia && datos.LatenciaMs > 0)
            partes.Add($"{datos.LatenciaMs:F1}ms");
        if (_verFrecuencia && datos.Hz > 0)
            partes.Add($"{datos.Hz}Hz");
        if (_verTipo && !string.IsNullOrEmpty(datos.TipoMando))
            partes.Add(datos.TipoMando);

        _detalles!.Text = partes.Count > 0 ? "· " + string.Join(" · ", partes) : "";
    }

    private void AplicarDetallado(InfoMando datos)
    {
        _avatarLetras!.Text = ObtenerIniciales(datos.Nombre);
        _titulo!.Text = datos.Nombre;
        _puntoConexion!.Background = DisenadorColores.EstadoConexionBrush(datos.Conectado);

        if (_verBateria && datos.NivelBateria >= 0)
        {
            var ancho = datos.NivelBateria / 100.0 * 60;
            _barraNivel!.Width = ancho;
            _barraNivel.Background = DisenadorColores.NivelBateria(datos.NivelBateria);
            _textoPorcentaje!.Text = $"{datos.NivelBateria}%{(datos.Cargando ? " ⚡" : "")}";
            _barraNivel.Visibility = Visibility.Visible;
            _textoPorcentaje.Visibility = Visibility.Visible;
        }
        else
        {
            _barraNivel!.Visibility = Visibility.Collapsed;
            _textoPorcentaje!.Visibility = Visibility.Collapsed;
        }

        var detalles = new List<string>();
        if (_verLatencia && datos.LatenciaMs > 0)
            detalles.Add($"Lat: {datos.LatenciaMs:F1}ms");
        if (_verFrecuencia && datos.Hz > 0)
            detalles.Add($"{datos.Hz}Hz");
        if (_verPerfil && !string.IsNullOrEmpty(datos.PerfilActivo))
            detalles.Add($"Perfil: {datos.PerfilActivo}");
        if (_verTipo && !string.IsNullOrEmpty(datos.TipoMando))
            detalles.Add(datos.TipoMando);
        _detalles!.Text = detalles.Count > 0 ? string.Join(" · ", detalles) : "";
    }

    public void Reconfigurar(string hexAcento, string hexFondo, double escala,
        bool verBateria, bool verLatencia, bool verFrecuencia, bool verPerfil, bool verConexion, bool verTipo)
    {
        _acentoHex = hexAcento;
        _bgHex = hexFondo;
        _escala = escala;
        _verBateria = verBateria;
        _verLatencia = verLatencia;
        _verFrecuencia = verFrecuencia;
        _verPerfil = verPerfil;
        _verConexion = verConexion;
        _verTipo = verTipo;

        if (_estilo != EstiloTarjeta.Minimal && _estilo != EstiloTarjeta.Terminal && _estilo != EstiloTarjeta.InputVisualizer && _estilo != EstiloTarjeta.Linea)
            _cajon.Background = DisenadorColores.CrearFondo(hexFondo);

        _cajon.MinWidth = ObtenerMinWidth() * _escala;
        _cajon.MaxWidth = ObtenerMaxWidth() * _escala;

        if (_avatar != null)
            _avatar.Background = DisenadorColores.CrearAcento(hexAcento);

        if (_avatarLetras != null && _estilo == EstiloTarjeta.Minimal)
            _avatarLetras.Foreground = DisenadorColores.CrearAcento(hexAcento);

        if (_dotIzq != null)
            _dotIzq.Background = DisenadorColores.CrearAcento(hexAcento);
        if (_dotDer != null)
            _dotDer.Background = DisenadorColores.CrearAcento(hexAcento);
        if (_nvlL2 != null)
            _nvlL2.Background = DisenadorColores.CrearAcento(hexAcento);
        if (_nvlR2 != null)
            _nvlR2.Background = DisenadorColores.CrearAcento(hexAcento);

        if (_titulo != null)
            _titulo.FontSize = 13 * escala;

        if (_detalles != null)
            _detalles.FontSize = 11 * escala;

        if (_estilo == EstiloTarjeta.InputVisualizer)
            EscalarInputVisualizer();

        if (_textoLatencia != null)
            _textoLatencia.FontSize = 11 * escala;
    }

    public void Reconfigurar(OverlayCardConfig cfg, double escala)
    {
        if (!string.IsNullOrEmpty(cfg.ColorAcento))
            _acentoHex = cfg.ColorAcento;
        if (!string.IsNullOrEmpty(cfg.ColorFondo))
            _bgHex = cfg.ColorFondo;
        _escala = cfg.Escala ?? escala;
        _verBateria = cfg.VerBateria;
        _verLatencia = cfg.VerLatencia;
        _verFrecuencia = cfg.VerFrecuencia;
        _verPerfil = cfg.VerPerfil;
        _verConexion = cfg.VerConexion;
        _verTipo = cfg.VerTipo;

        if (_estilo != EstiloTarjeta.Minimal && _estilo != EstiloTarjeta.Terminal && _estilo != EstiloTarjeta.InputVisualizer && _estilo != EstiloTarjeta.Linea)
            _cajon.Background = DisenadorColores.CrearFondo(_bgHex);

        _cajon.MinWidth = ObtenerMinWidth() * _escala;
        _cajon.MaxWidth = ObtenerMaxWidth() * _escala;

        if (_avatar != null)
            _avatar.Background = DisenadorColores.CrearAcento(_acentoHex);

        if (_avatarLetras != null && _estilo == EstiloTarjeta.Minimal)
            _avatarLetras.Foreground = DisenadorColores.CrearAcento(_acentoHex);

        if (_dotIzq != null)
            _dotIzq.Background = DisenadorColores.CrearAcento(_acentoHex);
        if (_dotDer != null)
            _dotDer.Background = DisenadorColores.CrearAcento(_acentoHex);
        if (_nvlL2 != null)
            _nvlL2.Background = DisenadorColores.CrearAcento(_acentoHex);
        if (_nvlR2 != null)
            _nvlR2.Background = DisenadorColores.CrearAcento(_acentoHex);

        if (_titulo != null)
            _titulo.FontSize = 13 * escala;

        if (_detalles != null)
            _detalles.FontSize = 11 * escala;

        if (_estilo == EstiloTarjeta.InputVisualizer)
            EscalarInputVisualizer();
    }

    private void EscalarInputVisualizer()
    {
        _stickIzq!.Width = _stickIzq.Height = 50 * _escala;
        _stickDer!.Width = _stickDer.Height = 50 * _escala;
        _dotIzq!.Width = _dotIzq.Height = 8 * _escala;
        _dotDer!.Width = _dotDer.Height = 8 * _escala;
        _trkL2!.MinWidth = 60 * _escala;
        _trkL2.Height = 6 * _escala;
        _trkR2!.MinWidth = 60 * _escala;
        _trkR2.Height = 6 * _escala;
        _nvlL2!.Height = 6 * _escala;
        _nvlR2!.Height = 6 * _escala;
        _btnA!.Width = _btnA.Height = 22 * _escala;
        _btnB!.Width = _btnB.Height = 22 * _escala;
        _btnX!.Width = _btnX.Height = 22 * _escala;
        _btnY!.Width = _btnY.Height = 22 * _escala;
        _txtL2!.FontSize = 9 * _escala;
        _txtR2!.FontSize = 9 * _escala;

        if (_titulo != null) _titulo.FontSize = 12 * _escala;
        if (_textoBateria != null) _textoBateria.FontSize = 10 * _escala;

        foreach (var btn in new[] { _btnA, _btnB, _btnX, _btnY })
        {
            if (btn?.Child is TextBlock tb)
                tb.FontSize = 10 * _escala;
        }
    }
}
