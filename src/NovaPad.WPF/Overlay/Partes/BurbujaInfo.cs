using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NovaPad.WPF.Overlay.Dibujo;

namespace NovaPad.WPF.Overlay.Partes;

public enum EstiloAviso { Clasica, Moderna, Minimal, Compacta }

public class BurbujaInfo
{
    private readonly Border _contenedor;
    private readonly Border _barraAcento;
    private readonly Border _icono;
    private readonly TextBlock _linea1;
    private readonly TextBlock _linea2;
    private EstiloAviso _estilo;
    private bool _activa;

    public FrameworkElement Vista => _contenedor;
    public bool Ocupada => _activa;

    public BurbujaInfo(string acentoHex, EstiloAviso estilo = EstiloAviso.Clasica)
    {
        _estilo = estilo;
        var acento = DisenadorColores.CrearAcento(acentoHex);

        _linea2 = new TextBlock
        {
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.Wrap
        };

        _linea1 = new TextBlock
        {
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        _barraAcento = new Border
        {
            CornerRadius = new CornerRadius(2),
            Background = acento,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _icono = new Border
        {
            Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
            Background = acento,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var sombra = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.Black, Opacity = 0.4, BlurRadius = 14, ShadowDepth = 4, Direction = -45
        };

        _contenedor = new Border
        {
            Margin = new Thickness(0, 0, 0, 8),
            MinWidth = 220, MaxWidth = 380,
            Opacity = 0,
            RenderTransformOrigin = new Point(0.5, 0.5),
            Effect = sombra
        };
        _contenedor.RenderTransform = new TranslateTransform();

        AplicarEstilo();

        _contenedor.IsVisibleChanged += (_, _) =>
        {
            _barraAcento.Width = 0;
        };
    }

    private void AplicarEstilo()
    {
        var acento = _barraAcento.Background;
        _contenedor.Child = null;

        switch (_estilo)
        {
            case EstiloAviso.Moderna:
            {
                _linea1.FontSize = 14;
                _linea1.Foreground = new SolidColorBrush(Colors.White);
                _linea2.FontSize = 12;
                _linea2.Foreground = DisenadorColores.CrearTextoSecundario();

                _barraAcento.Width = 5;
                _barraAcento.CornerRadius = new CornerRadius(0, 0, 0, 0);
                _barraAcento.Margin = new Thickness(0);

                var columna = new StackPanel { Margin = new Thickness(14, 0, 0, 0) };
                columna.Children.Add(_linea1);
                columna.Children.Add(_linea2);

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(_barraAcento, 0);
                grid.Children.Add(_barraAcento);
                Grid.SetColumn(columna, 1);
                grid.Children.Add(columna);

                _contenedor.Background = new SolidColorBrush(Color.FromArgb(210, 0x1E, 0x1E, 0x2E));
                _contenedor.BorderBrush = null;
                _contenedor.BorderThickness = new Thickness(0);
                _contenedor.CornerRadius = new CornerRadius(0, 10, 10, 0);
                _contenedor.Padding = new Thickness(12, 10, 16, 10);
                break;
            }
            case EstiloAviso.Minimal:
            {
                _linea1.FontSize = 13;
                _linea1.Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                _linea2.FontSize = 11;
                _linea2.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));

                var columna = new StackPanel();
                columna.Children.Add(_linea1);
                columna.Children.Add(_linea2);

                _contenedor.Background = new SolidColorBrush(Color.FromArgb(200, 0x11, 0x11, 0x11));
                _contenedor.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0xFF));
                _contenedor.BorderThickness = new Thickness(1);
                _contenedor.CornerRadius = new CornerRadius(6);
                _contenedor.Padding = new Thickness(12, 8, 12, 8);
                _contenedor.Child = columna;
                break;
            }
            case EstiloAviso.Compacta:
            {
                _linea1.FontSize = 11;
                _linea1.Foreground = DisenadorColores.CrearTexto();
                _linea2.FontSize = 10;
                _linea2.Foreground = DisenadorColores.CrearTextoSecundario();

                _icono.Width = 6;
                _icono.Height = 6;
                _icono.CornerRadius = new CornerRadius(3);
                _icono.Margin = new Thickness(0, 3, 0, 0);

                var columna = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
                columna.Children.Add(_linea1);
                columna.Children.Add(_linea2);

                var pila = new StackPanel { Orientation = Orientation.Horizontal };
                pila.Children.Add(_icono);
                pila.Children.Add(columna);

                _contenedor.Background = DisenadorColores.CrearFondo("#222222");
                _contenedor.BorderBrush = DisenadorColores.CrearBorde();
                _contenedor.BorderThickness = new Thickness(1);
                _contenedor.CornerRadius = new CornerRadius(8);
                _contenedor.Padding = new Thickness(10, 6, 12, 6);
                _contenedor.MinWidth = 160;
                _contenedor.Child = pila;
                break;
            }
            default: // Clasica
            {
                _linea1.FontSize = 13;
                _linea1.Foreground = DisenadorColores.CrearTexto();
                _linea2.FontSize = 12;
                _linea2.Foreground = DisenadorColores.CrearTextoSecundario();

                _barraAcento.Width = 4;
                _barraAcento.CornerRadius = new CornerRadius(2);
                _barraAcento.Margin = new Thickness(0);

                var columna = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
                columna.Children.Add(_linea1);
                columna.Children.Add(_linea2);

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(_barraAcento, 0);
                grid.Children.Add(_barraAcento);
                Grid.SetColumn(columna, 1);
                grid.Children.Add(columna);

                _contenedor.Background = DisenadorColores.CrearFondo("#222222");
                _contenedor.BorderBrush = DisenadorColores.CrearBorde();
                _contenedor.BorderThickness = new Thickness(1);
                _contenedor.CornerRadius = new CornerRadius(10);
                _contenedor.Padding = new Thickness(14, 10, 14, 10);
                _contenedor.Child = grid;
                break;
            }
        }
    }

    public void Mostrar(string titulo, string cuerpo, double duracion)
    {
        _activa = true;
        _linea1.Text = titulo;
        _linea2.Text = cuerpo;

        _contenedor.BeginAnimation(UIElement.OpacityProperty, null);
        _contenedor.RenderTransform.BeginAnimation(TranslateTransform.YProperty, null);

        if (_estilo != EstiloAviso.Minimal)
        {
            var anchoBarra = new DoubleAnimation(0, _estilo == EstiloAviso.Moderna ? 5 : 4, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            _barraAcento.BeginAnimation(FrameworkElement.WidthProperty, anchoBarra);
        }

        var animEntrada = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _contenedor.BeginAnimation(UIElement.OpacityProperty, animEntrada);

        var animDeslizar = new DoubleAnimation(-20, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _contenedor.RenderTransform.BeginAnimation(TranslateTransform.YProperty, animDeslizar);
    }

    public void Ocultar()
    {
        _activa = false;

        if (_estilo != EstiloAviso.Minimal)
        {
            var anchoBarra = new DoubleAnimation(4, 0, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            _barraAcento.BeginAnimation(FrameworkElement.WidthProperty, anchoBarra);
        }

        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
        _contenedor.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    public void CambiarAcento(string hex)
    {
        var acento = DisenadorColores.CrearAcento(hex);
        _barraAcento.Background = acento;
        _icono.Background = acento;
    }
}
