using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using Windows.Storage.Streams;
using NovaPad.WPF.Overlay.Dibujo;
using Serilog;

namespace NovaPad.WPF.Overlay.Partes;

public class PanelControlMultimedia
{
    private readonly Border _marco;
    private readonly StackPanel _contenido;
    private readonly TextBlock _tituloTexto;
    private readonly TextBlock _artistaTexto;
    private readonly Border _miniatura;
    private readonly Image _imagenMiniatura;
    private readonly Button _btnAnterior;
    private readonly Button _btnPlayPause;
    private readonly Button _btnSiguiente;
    private readonly Border _barraProgresoTrack;
    private readonly Border _barraProgresoNivel;
    private readonly TextBlock _tiempoTexto;
    private readonly DispatcherTimer _timerProgreso;
    private GlobalSystemMediaTransportControlsSession? _sesion;
    private string _acentoHex;
    private bool _reproduciendo;

    public UIElement Vista => _marco;

    public PanelControlMultimedia(string hexAcento)
    {
        _acentoHex = hexAcento;

        _tituloTexto = new TextBlock
        {
            Text = "Sin reproduccion",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = DisenadorColores.CrearTexto(),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 260
        };

        _artistaTexto = new TextBlock
        {
            Text = "Selecciona una fuente de audio",
            FontSize = 11,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 260
        };

        var infoPila = new StackPanel();
        infoPila.Children.Add(_tituloTexto);
        infoPila.Children.Add(_artistaTexto);

        _imagenMiniatura = new Image
        {
            Width = 40,
            Height = 40,
            Stretch = Stretch.UniformToFill
        };

        _miniatura = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(40, 0xFF, 0xFF, 0xFF)),
            Child = _imagenMiniatura,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };

        var filaInfo = new StackPanel { Orientation = Orientation.Horizontal };
        filaInfo.Children.Add(_miniatura);
        filaInfo.Children.Add(infoPila);

        _barraProgresoNivel = new Border
        {
            Height = 3,
            CornerRadius = new CornerRadius(1.5f),
            Background = DisenadorColores.CrearAcento(_acentoHex),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0
        };

        _barraProgresoTrack = new Border
        {
            Height = 3,
            CornerRadius = new CornerRadius(1.5f),
            Background = new SolidColorBrush(Color.FromArgb(40, 0xFF, 0xFF, 0xFF)),
            Child = _barraProgresoNivel,
            Margin = new Thickness(0, 4, 0, 8)
        };

        _tiempoTexto = new TextBlock
        {
            Text = "0:00 / 0:00",
            FontSize = 9,
            Foreground = DisenadorColores.CrearTextoSecundario(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };

        _btnAnterior = CrearBotonControl("\u23EE");
        _btnAnterior.Click += (_, _) => EnviarAnterior();
        _btnPlayPause = CrearBotonControl("\u25B6");
        _btnPlayPause.Click += (_, _) => EnviarPlayPause();
        _btnSiguiente = CrearBotonControl("\u23ED");
        _btnSiguiente.Click += (_, _) => EnviarSiguiente();

        var filaControles = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        filaControles.Children.Add(_btnAnterior);
        filaControles.Children.Add(new Border { Width = 16 });
        filaControles.Children.Add(_btnPlayPause);
        filaControles.Children.Add(new Border { Width = 16 });
        filaControles.Children.Add(_btnSiguiente);

        _contenido = new StackPanel();
        _contenido.Children.Add(filaInfo);
        _contenido.Children.Add(_barraProgresoTrack);
        _contenido.Children.Add(_tiempoTexto);
        _contenido.Children.Add(filaControles);

        _marco = new Border
        {
            Child = _contenido,
            Background = DisenadorColores.CrearFondo("#161622"),
            BorderBrush = new SolidColorBrush(Color.FromArgb(50, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 12, 16, 14),
            Visibility = Visibility.Collapsed
        };

        _timerProgreso = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timerProgreso.Tick += (_, _) =>
        {
            _ = ActualizarInfoPista();
            _ = ActualizarInfoReproduccion();
            _ = ActualizarProgreso();
        };

        _ = IniciarAsync();
    }

    private Button CrearBotonControl(string contenido)
    {
        var btn = new Button
        {
            Content = contenido,
            Width = 40,
            Height = 40,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = DisenadorColores.CrearAcento(_acentoHex),
            Background = new SolidColorBrush(Color.FromArgb(30, 0xFF, 0xFF, 0xFF)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        btn.ApplyTemplate();
        if (btn.Template?.FindName("Border", btn) is Border border)
        {
            border.CornerRadius = new CornerRadius(20);
        }

        return btn;
    }

    private async Task IniciarAsync()
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (manager == null) return;

            manager.CurrentSessionChanged += ManagerOnCurrentSessionChanged;

            var session = manager.GetCurrentSession();
            ActualizarSesion(session);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "[MediaControl] Failed to initialize");
        }
    }

    private void ManagerOnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var session = sender.GetCurrentSession();
            ActualizarSesion(session);
        });
    }

    private void ActualizarSesion(GlobalSystemMediaTransportControlsSession? nuevaSesion)
    {
        if (_sesion != null)
        {
            _sesion.MediaPropertiesChanged -= SesionOnMediaPropertiesChanged;
            _sesion.PlaybackInfoChanged -= SesionOnPlaybackInfoChanged;
            _sesion.TimelinePropertiesChanged -= SesionOnTimelinePropertiesChanged;
        }

        _sesion = nuevaSesion;

        if (_sesion == null)
        {
            _marco.Visibility = Visibility.Collapsed;
            return;
        }

        _sesion.MediaPropertiesChanged += SesionOnMediaPropertiesChanged;
        _sesion.PlaybackInfoChanged += SesionOnPlaybackInfoChanged;
        _sesion.TimelinePropertiesChanged += SesionOnTimelinePropertiesChanged;

        _marco.Visibility = Visibility.Visible;
        _ = ActualizarInfoPista();
        _ = ActualizarInfoReproduccion();
        _ = ActualizarProgreso();
        _timerProgreso.Start();
    }

    private async void SesionOnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () => await ActualizarInfoPista());
    }

    private async void SesionOnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await ActualizarInfoReproduccion();
            await ActualizarProgreso();
        });
    }

    private async void SesionOnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await ActualizarInfoReproduccion();
            await ActualizarProgreso();
        });
    }

    private async Task ActualizarInfoPista()
    {
        if (_sesion == null) return;
        try
        {
            var props = await _sesion.TryGetMediaPropertiesAsync();
            if (props == null) return;

            _tituloTexto.Text = string.IsNullOrEmpty(props.Title) ? "Desconocido" : props.Title;
            _artistaTexto.Text = string.IsNullOrEmpty(props.Artist)
                ? (string.IsNullOrEmpty(props.AlbumArtist) ? "Fuente desconocida" : props.AlbumArtist)
                : props.Artist;

            if (props.Thumbnail != null)
            {
                await CargarMiniatura(props.Thumbnail);
            }
            else
            {
                _imagenMiniatura.Source = null;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "[MediaControl] Error getting media properties");
        }
    }

    private async Task CargarMiniatura(IRandomAccessStreamReference thumbnailRef)
    {
        try
        {
            var stream = await thumbnailRef.OpenReadAsync();
            if (stream == null || stream.Size == 0) return;

            using var winRtStream = stream.AsStreamForRead();
            var buffer = new byte[winRtStream.Length];
            _ = await winRtStream.ReadAsync(buffer, 0, buffer.Length);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(buffer);
            bitmap.EndInit();
            bitmap.Freeze();
            _imagenMiniatura.Source = bitmap;
        }
        catch
        {
            _imagenMiniatura.Source = null;
        }
    }

    private Task ActualizarInfoReproduccion()
    {
        if (_sesion == null) return Task.CompletedTask;
        try
        {
            var info = _sesion.GetPlaybackInfo();
            if (info == null) return Task.CompletedTask;

            _reproduciendo = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            _btnPlayPause.Content = _reproduciendo ? "\u23F8" : "\u25B6";

            var timeline = _sesion.GetTimelineProperties();
            if (timeline == null) return Task.CompletedTask;

            var maxBarra = _barraProgresoTrack.ActualWidth > 0 ? _barraProgresoTrack.ActualWidth : 260;

            if (timeline.EndTime > timeline.Position && timeline.EndTime > TimeSpan.Zero)
            {
                var progreso = timeline.Position.TotalMilliseconds / timeline.EndTime.TotalMilliseconds;
                _barraProgresoNivel.Width = progreso * maxBarra;
            }
        }
        catch
        {
        }
        return Task.CompletedTask;
    }

    private Task ActualizarProgreso()
    {
        if (_sesion == null || !_reproduciendo) return Task.CompletedTask;
        try
        {
            var timeline = _sesion.GetTimelineProperties();
            if (timeline == null) return Task.CompletedTask;

            var maxBarra = _barraProgresoTrack.ActualWidth > 0 ? _barraProgresoTrack.ActualWidth : 260;

            if (timeline.EndTime > TimeSpan.Zero && timeline.Position <= timeline.EndTime)
            {
                var progreso = timeline.Position.TotalMilliseconds / timeline.EndTime.TotalMilliseconds;
                _barraProgresoNivel.Width = progreso * maxBarra;
                _tiempoTexto.Text = $"{FormatearTiempo(timeline.Position)} / {FormatearTiempo(timeline.EndTime)}";
            }
        }
        catch
        {
        }
        return Task.CompletedTask;
    }

    private static string FormatearTiempo(TimeSpan ts)
    {
        return ts.Hours > 0
            ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
    private const byte VK_MEDIA_PREV_TRACK = 0xB1;

    private static void EnviarTeclaMedia(byte vk)
    {
        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private void EnviarPlayPause()
    {
        EnviarTeclaMedia(VK_MEDIA_PLAY_PAUSE);
    }

    private void EnviarAnterior()
    {
        EnviarTeclaMedia(VK_MEDIA_PREV_TRACK);
    }

    private void EnviarSiguiente()
    {
        EnviarTeclaMedia(VK_MEDIA_NEXT_TRACK);
    }

    public void CambiarAcento(string hexAcento)
    {
        _acentoHex = hexAcento;
        _barraProgresoNivel.Background = DisenadorColores.CrearAcento(hexAcento);
        _btnAnterior.Foreground = DisenadorColores.CrearAcento(hexAcento);
        _btnPlayPause.Foreground = DisenadorColores.CrearAcento(hexAcento);
        _btnSiguiente.Foreground = DisenadorColores.CrearAcento(hexAcento);
    }

    public void Detener()
    {
        _timerProgreso.Stop();
        if (_sesion != null)
        {
            _sesion.MediaPropertiesChanged -= SesionOnMediaPropertiesChanged;
            _sesion.PlaybackInfoChanged -= SesionOnPlaybackInfoChanged;
            _sesion.TimelinePropertiesChanged -= SesionOnTimelinePropertiesChanged;
        }
    }
}
