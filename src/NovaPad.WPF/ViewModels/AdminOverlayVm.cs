using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Events;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;
using NovaPad.WPF.Overlay;
using NovaPad.WPF.Services;
using Serilog;

namespace NovaPad.WPF.ViewModels;

public record OptionMando(string Id, string Nombre, bool EsGlobal)
{
    public string Display => EsGlobal ? "Global" : Nombre;
}

public partial class AdminOverlayVm : ObservableObject
{
    private readonly IAppSettingsService _settings;
    private readonly IOverlayService _overlay;
    private readonly IControllerManagerService _controllers;
    private readonly IInputProcessingService _inputProcessing;
    private readonly INotificationService _notifications;
    private bool _loading;
    private bool _starting;

    [ObservableProperty] private double _opacity = 0.8;
    [ObservableProperty] private double _scale = 1.0;
    [ObservableProperty] private double _notifDuration = 3.0;
    [ObservableProperty] private bool _showBattery = true;
    [ObservableProperty] private bool _showLatency = true;
    [ObservableProperty] private bool _showPolling;
    [ObservableProperty] private bool _showProfile = true;
    [ObservableProperty] private bool _showConnection = true;
    [ObservableProperty] private bool _showSignal = true;
    [ObservableProperty] private bool _showFps;
    [ObservableProperty] private bool _showClock = true;
    [ObservableProperty] private bool _showType = true;
    [ObservableProperty] private bool _showNotifs = true;
    [ObservableProperty] private bool _showConnectionNotifications = true;
    [ObservableProperty] private bool _showBatteryNotifications = true;
    [ObservableProperty] private string _statusText = "Detenido";
    [ObservableProperty] private bool _canStart = true;
    [ObservableProperty] private bool _canStop;
    [ObservableProperty] private int _hudAnchorIdx;
    [ObservableProperty] private double _hudOffX = 20;
    [ObservableProperty] private double _hudOffY = 20;
    [ObservableProperty] private int _notifAnchorIdx = 6;
    [ObservableProperty] private double _notifOffX = 20;
    [ObservableProperty] private double _notifOffY = 10;
    [ObservableProperty] private string _accent = "#00BCD4";
    [ObservableProperty] private double _fontScale = 1.0;
    [ObservableProperty] private string _bg = "#111111";
    [ObservableProperty] private int _accentIdx;
    [ObservableProperty] private int _bgIdx;
    [ObservableProperty] private int _estiloTarjetaIdx;
    [ObservableProperty] private int _estiloAvisoIdx;

    [ObservableProperty] private OptionMando? _selectedController;

    public bool IsPerController => SelectedController is { EsGlobal: false };

    public ObservableCollection<OptionMando> ControllerOptions { get; } = new();

    public string[] PaletteAccent { get; } =
    {
        "#00BCD4", "#2196F3", "#3F51B5", "#9C27B0",
        "#E91E63", "#F44336", "#FF9800", "#4CAF50"
    };

    public string[] PaletteBg { get; } =
    {
        "#111111", "#1A1A2E", "#1B1B2F", "#222831",
        "#2C2C2C", "#333333", "#0D0D0D", "#1E1E2E"
    };

    public string[] Anchors { get; } =
    {
        "TopRight", "TopCenter", "TopLeft",
        "CenterRight", "Center", "CenterLeft",
        "BottomRight", "BottomCenter", "BottomLeft"
    };

    public string[] AnchorLabels { get; } =
    {
        "Arriba Derecha", "Arriba Centro", "Arriba Izquierda",
        "Centro Derecha", "Centro", "Centro Izquierda",
        "Abajo Derecha", "Abajo Centro", "Abajo Izquierda"
    };

    public string[] EstilosTarjeta { get; } =
    {
        "Clasico", "Moderno", "Mini", "Terminal",
        "Minimal", "SoloBateria", "CompactoVertical", "InputVisualizer", "Linea", "Detallado"
    };

    public string[] EstiloLabels { get; } =
    {
        "Clásico", "Moderno", "Mini", "Terminal",
        "Minimal", "Solo Batería", "Compacto", "Input Visualizer", "Línea", "Detallado"
    };

    public string[] EstilosAviso { get; } =
    {
        "Clasica", "Moderna", "Minimal", "Compacta"
    };

    public string[] EstiloAvisoLabels { get; } =
    {
        "Clásica", "Moderna", "Minimal", "Compacta"
    };

    public AdminOverlayVm(IAppSettingsService settings, IOverlayService overlay, IControllerManagerService controllers, IInputProcessingService inputProcessing, INotificationService notifications)
    {
        _settings = settings;
        _overlay = overlay;
        _controllers = controllers;
        _inputProcessing = inputProcessing;
        _notifications = notifications;
        _starting = false;
        _controllers.ControllerConnected += (_, _) => { RefrescarOpciones(); PushHudIfActive(); };
        _controllers.ControllerDisconnected += (_, _) => { RefrescarOpciones(); PushHudIfActive(); };
        _controllers.ControllerUpdated += (_, _) => PushHudIfActive();
        RefrescarOpciones();
        RefreshSettings();
        RefreshStatus();
    }

    private void PushHudIfActive()
    {
        if (_overlay.IsActive)
            PushHud();
    }

    private void RefrescarOpciones()
    {
        var prevId = SelectedController?.Id;
        ControllerOptions.Clear();
        ControllerOptions.Add(new OptionMando("", "Global", true));
        foreach (var c in _controllers.ConnectedControllers.Where(c => IsMandoVisible(c)))
            ControllerOptions.Add(new OptionMando(c.Id, $"{c.EffectiveName}", false));

        if (prevId != null && ControllerOptions.Any(o => o.Id == prevId))
            SelectedController = ControllerOptions.First(o => o.Id == prevId);
        else
            SelectedController = ControllerOptions[0];
    }

    partial void OnSelectedControllerChanged(OptionMando? value)
    {
        RefreshSettings();
        OnPropertyChanged(nameof(IsPerController));
    }

    partial void OnAccentChanged(string value) { if (_loading) return; AccentIdx = System.Array.IndexOf(PaletteAccent, value); GuardarTarjetaYEnviar(); }
    partial void OnBgChanged(string value) { if (_loading) return; BgIdx = System.Array.IndexOf(PaletteBg, value); GuardarTarjetaYEnviar(); }
    partial void OnFontScaleChanged(double value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnAccentIdxChanged(int value) { if (_loading) return; if (value >= 0 && value < PaletteAccent.Length) Accent = PaletteAccent[value]; }
    partial void OnBgIdxChanged(int value) { if (_loading) return; if (value >= 0 && value < PaletteBg.Length) Bg = PaletteBg[value]; }
    partial void OnOpacityChanged(double value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnNotifDurationChanged(double value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnShowBatteryChanged(bool value) { if (_loading) return; GuardarTarjetaYEnviar(); }
    partial void OnShowLatencyChanged(bool value) { if (_loading) return; GuardarTarjetaYEnviar(); }
    partial void OnShowPollingChanged(bool value) { if (_loading) return; GuardarTarjetaYEnviar(); }
    partial void OnShowProfileChanged(bool value) { if (_loading) return; GuardarTarjetaYEnviar(); }
    partial void OnShowConnectionChanged(bool value) { if (_loading) return; GuardarTarjetaYEnviar(); }
    partial void OnShowFpsChanged(bool value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnShowClockChanged(bool value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnShowSignalChanged(bool value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnShowTypeChanged(bool value) { if (_loading) return; GuardarTarjetaYEnviar(); }
    partial void OnShowNotifsChanged(bool value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnShowConnectionNotificationsChanged(bool value) { if (_loading) return; SaveGlobal(); }
    partial void OnShowBatteryNotificationsChanged(bool value) { if (_loading) return; SaveGlobal(); }
    partial void OnScaleChanged(double value) { if (_loading) return; GuardarPosicionYEnviar(); }
    partial void OnHudAnchorIdxChanged(int value) { if (_loading) return; GuardarPosicionYEnviar(); }
    partial void OnHudOffXChanged(double value) { if (_loading) return; GuardarPosicionYEnviar(); }
    partial void OnHudOffYChanged(double value) { if (_loading) return; GuardarPosicionYEnviar(); }
    partial void OnNotifAnchorIdxChanged(int value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnNotifOffXChanged(double value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnNotifOffYChanged(double value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }
    partial void OnEstiloTarjetaIdxChanged(int value) { if (_loading) return; GuardarTarjetaYEnviar(); }
    partial void OnEstiloAvisoIdxChanged(int value) { if (_loading) return; SaveGlobal(); PushCfg(); PushHud(); }

    private void GuardarTarjetaYEnviar()
    {
        SaveTarjeta();
        PushCfg();
        PushCardCfg();
        PushHud();
    }

    private void GuardarPosicionYEnviar()
    {
        if (IsPerController && SelectedController != null)
        {
            var pc = _settings.Settings.Overlay.PorMando.GetValueOrDefault(SelectedController.Id)
                     ?? new OverlayCardConfig { ControllerId = SelectedController.Id };
            pc.AnclajeHud = Anchors[HudAnchorIdx];
            pc.DesvioX = HudOffX;
            pc.DesvioY = HudOffY;
            pc.Escala = Scale;
            _settings.Settings.Overlay.PorMando[SelectedController.Id] = pc;
            _settings.Save();
            PushCardCfg();
        }
        else
        {
            SaveGlobal();
        }
        PushCfg();
        PushHud();
    }

    public void RefreshSettings()
    {
        _loading = true;
        var s = _settings.Settings;

        Opacity = s.Overlay.Opacidad;
        Scale = s.Overlay.Escala;
        FontScale = s.Overlay.TamanoLetra;
        NotifDuration = s.Notifications.DurationSeconds;
        HudOffX = s.Overlay.PosX;
        HudOffY = s.Overlay.PosY;
        var idx = System.Array.IndexOf(Anchors, s.Overlay.Anclaje);
        HudAnchorIdx = idx >= 0 ? idx : 0;
        ShowNotifs = s.Overlay.VerAvisos;
        ShowConnectionNotifications = s.Notifications.ShowConnectionNotifications;
        ShowBatteryNotifications = s.Notifications.ShowBatteryNotifications;
        var nidx = System.Array.IndexOf(Anchors, s.Overlay.AnclajeAvisos);
        NotifAnchorIdx = nidx >= 0 ? nidx : 6;
        NotifOffX = s.Overlay.DesvioAvisoX;
        NotifOffY = s.Overlay.DesvioAvisoY;
        ShowFps = s.Overlay.VerFps;
        ShowClock = s.Overlay.VerReloj;
        ShowSignal = s.Overlay.VerSenal;

        if (IsPerController && SelectedController != null && s.Overlay.PorMando.TryGetValue(SelectedController.Id, out var pc))
        {
            ShowBattery = pc.VerBateria;
            ShowLatency = pc.VerLatencia;
            ShowPolling = pc.VerFrecuencia;
            ShowProfile = pc.VerPerfil;
            ShowConnection = pc.VerConexion;
            ShowType = pc.VerTipo;
            ShowSignal = pc.VerSenal;
            if (!string.IsNullOrEmpty(pc.ColorAcento)) Accent = pc.ColorAcento;
            if (!string.IsNullOrEmpty(pc.ColorFondo)) Bg = pc.ColorFondo;
            var eidx = System.Array.IndexOf(EstilosTarjeta, pc.EstiloTarjeta);
            EstiloTarjetaIdx = eidx >= 0 ? eidx : 0;
            if (pc.AnclajeHud != null)
            {
                var aidx = System.Array.IndexOf(Anchors, pc.AnclajeHud);
                if (aidx >= 0) HudAnchorIdx = aidx;
            }
            if (pc.DesvioX != null) HudOffX = pc.DesvioX.Value;
            if (pc.DesvioY != null) HudOffY = pc.DesvioY.Value;
            if (pc.Escala != null) Scale = pc.Escala.Value;
        }
        else
        {
            ShowBattery = s.Overlay.VerBateria;
            ShowLatency = s.Overlay.VerLatencia;
            ShowPolling = s.Overlay.VerFrecuencia;
            ShowProfile = s.Overlay.VerPerfil;
            ShowConnection = s.Overlay.VerConexion;
            ShowType = s.Overlay.VerTipo;
            ShowSignal = s.Overlay.VerSenal;
            Accent = s.Overlay.ColorAcento;
            Bg = s.Overlay.ColorFondo;
            var eidx = System.Array.IndexOf(EstilosTarjeta, s.Overlay.EstiloTarjeta);
            EstiloTarjetaIdx = eidx >= 0 ? eidx : 0;
        }

        var estAvIdx = System.Array.IndexOf(EstilosAviso, s.Overlay.EstiloAviso);
        EstiloAvisoIdx = estAvIdx >= 0 ? estAvIdx : 0;

        AccentIdx = System.Array.IndexOf(PaletteAccent, Accent);
        BgIdx = System.Array.IndexOf(PaletteBg, Bg);

        _loading = false;
    }

    private void RefreshStatus()
    {
        if (_starting)
        {
            StatusText = "Iniciando...";
            CanStart = false;
            CanStop = false;
        }
        else if (_overlay.IsActive)
        {
            var loc = LocalizationService.Instance;
            StatusText = loc["OverlaySettings_Running"];
            CanStart = false;
            CanStop = true;
        }
        else
        {
            var loc = LocalizationService.Instance;
            StatusText = loc["OverlaySettings_Stopped"];
            CanStart = true;
            CanStop = false;
        }
    }

    [RelayCommand] private void SetAccentColor(string hex) => Accent = hex;
    [RelayCommand] private void SetBgColor(string hex) => Bg = hex;
    [RelayCommand]
    private void TestAlert()
    {
        if (_overlay.IsActive)
            _overlay.ShowNotification("Notificacion", "Esto es una prueba");
        else
            _notifications.Show("Notificacion", "Esto es una prueba");
    }

    [RelayCommand]
    private void TestUpdate()
    {
        var dialog = new Views.UpdatePromptWindow("v9.9.9");
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        dialog.ShowDialog();
    }

    public void UpdateRunningState()
    {
        RefreshStatus();
    }

    [RelayCommand]
    private async Task StartLayerAsync()
    {
        Log.Information("[Overlay] StartLayerAsync: _starting={Starting}, IsActive={Active}", _starting, _overlay.IsActive);
        if (_starting || _overlay.IsActive)
        {
            Log.Information("[Overlay] StartLayerAsync skipped (already running/starting)");
            return;
        }

        _starting = true;
        RefreshStatus();

        try
        {
            // Mark as active BEFORE saving so auto-start persists immediately
            _settings.Settings.Overlay.Activado = true;
            _settings.Save();

            _overlay.ResetReadyState();

            try { _overlay.Show(); }
            catch (Exception ex)
            {
                Log.Error(ex, "[Overlay] Show failed");
                throw;
            }

            Log.Information("[Overlay] Waiting for UI initialization...");
            await _overlay.WaitUntilReadyAsync();
            Log.Information("[Overlay] UI initialized");

            // Push config from saved file values (no re-save) to preserve user settings
            try
            {
                Log.Information("[Overlay] PushCfg: PosX={PosX}, PosY={PosY}, Anchor={Anclaje}, Opacity={Opacidad}",
                    _settings.Settings.Overlay.PosX, _settings.Settings.Overlay.PosY,
                    _settings.Settings.Overlay.Anclaje, _settings.Settings.Overlay.Opacidad);
                PushCfg();
            }
            catch (Exception ex) { Log.Warning(ex, "[Overlay] PushCfg post-start failed"); }

            try
            {
                Log.Information("[Overlay] PushAllCardCfg: {Count} cards", _settings.Settings.Overlay.PorMando.Count);
                PushAllCardCfg();
            }
            catch (Exception ex) { Log.Warning(ex, "[Overlay] PushAllCardCfg post-start failed"); }

            try
            {
                var connected = _controllers.ConnectedControllers.Count(c => IsMandoVisible(c));
                Log.Information("[Overlay] PushHud: {Count} connected controllers", connected);
                PushHud();
            }
            catch (Exception ex) { Log.Warning(ex, "[Overlay] PushHud post-start failed"); }

            Log.Information("[Overlay] Started successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Overlay] Start failed");
        }
        finally
        {
            _starting = false;
            RefreshStatus();
        }
    }

    public async Task RestoreAsync()
    {
        Log.Information("[Overlay] RestoreAsync: IsActive={Active}, _starting={Starting}", _overlay.IsActive, _starting);
        if (_overlay.IsActive || _starting)
        {
            Log.Information("[Overlay] RestoreAsync skipped (already active or starting)");
            return;
        }

        Log.Information("[Overlay] Restoring from auto-start...");
        await StartLayerAsync();
        Log.Information("[Overlay] RestoreAsync completed");
    }

    [RelayCommand]
    private void StopLayer()
    {
        try
        {
            _overlay.Hide();
            _settings.Settings.Overlay.Activado = false;
            _settings.Save();
            Log.Information("[Overlay] Stopped");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Overlay] Stop failed");
        }
        RefreshStatus();
    }

    private void SaveGlobal()
    {
        var s = _settings.Settings;
        s.Notifications.DurationSeconds = NotifDuration;
        s.Notifications.ShowConnectionNotifications = ShowConnectionNotifications;
        s.Notifications.ShowBatteryNotifications = ShowBatteryNotifications;
        s.Overlay.Opacidad = Opacity;
        s.Overlay.Escala = Scale;
        s.Overlay.TamanoLetra = FontScale;
        s.Overlay.PosX = HudOffX;
        s.Overlay.PosY = HudOffY;
        s.Overlay.Anclaje = Anchors[HudAnchorIdx];
        s.Overlay.VerAvisos = ShowNotifs;
        s.Overlay.AnclajeAvisos = Anchors[NotifAnchorIdx];
        s.Overlay.DesvioAvisoX = NotifOffX;
        s.Overlay.DesvioAvisoY = NotifOffY;
        s.Overlay.VerFps = ShowFps;
        s.Overlay.VerReloj = ShowClock;
        s.Overlay.VerSenal = ShowSignal;
        s.Overlay.EstiloAviso = EstilosAviso[EstiloAvisoIdx];
        _settings.Save();
    }

    private void SaveTarjeta()
    {
        if (IsPerController && SelectedController != null)
        {
            var pc = _settings.Settings.Overlay.PorMando.GetValueOrDefault(SelectedController.Id)
                     ?? new OverlayCardConfig { ControllerId = SelectedController.Id };
            pc.VerBateria = ShowBattery;
            pc.VerLatencia = ShowLatency;
            pc.VerFrecuencia = ShowPolling;
            pc.VerPerfil = ShowProfile;
            pc.VerConexion = ShowConnection;
            pc.VerTipo = ShowType;
            pc.VerSenal = ShowSignal;
            pc.ColorAcento = Accent;
            pc.ColorFondo = Bg;
            pc.EstiloTarjeta = EstilosTarjeta[EstiloTarjetaIdx];
            pc.AnclajeHud = Anchors[HudAnchorIdx];
            pc.DesvioX = HudOffX;
            pc.DesvioY = HudOffY;
            pc.Escala = Scale;
            _settings.Settings.Overlay.PorMando[SelectedController.Id] = pc;
        }
        else
        {
            var s = _settings.Settings.Overlay;
            s.VerBateria = ShowBattery;
            s.VerLatencia = ShowLatency;
            s.VerFrecuencia = ShowPolling;
            s.VerPerfil = ShowProfile;
            s.VerConexion = ShowConnection;
            s.VerTipo = ShowType;
            s.VerSenal = ShowSignal;
            s.ColorAcento = Accent;
            s.ColorFondo = Bg;
            s.EstiloTarjeta = EstilosTarjeta[EstiloTarjetaIdx];
        }
        _settings.Save();
    }

    private void PushCfg()
    {
        var o = _settings.Settings.Overlay;
        Log.Information("[Overlay] PushCfg: PosX={PX}, PosY={PY}, Anchor={A}, VerBateria={VB}, VerAvisos={VA}, Accent={AC}",
            o.PosX, o.PosY, o.Anclaje, o.VerBateria, o.VerAvisos, o.ColorAcento);
        _overlay.ApplyConfig(new DatosConfigOverlay
        {
            Opacidad = o.Opacidad,
            Escala = o.Escala,
            DuracionAviso = _settings.Settings.Notifications.DurationSeconds,
            VerBateria = o.VerBateria,
            VerLatencia = o.VerLatencia,
            VerFrecuencia = o.VerFrecuencia,
            VerPerfil = o.VerPerfil,
            VerConexion = o.VerConexion,
            VerFps = o.VerFps,
            VerReloj = o.VerReloj,
            VerTipo = o.VerTipo,
            VerSenal = o.VerSenal,
            VerAvisos = o.VerAvisos,
            AnclajeHud = o.Anclaje,
            DesvioX = o.PosX,
            DesvioY = o.PosY,
            AnclajeAvisos = o.AnclajeAvisos,
            DesvioAvisoX = o.DesvioAvisoX,
            DesvioAvisoY = o.DesvioAvisoY,
            ColorAcento = o.ColorAcento,
            TamanoLetra = o.TamanoLetra,
            ColorFondo = o.ColorFondo,
            EstiloTarjeta = o.EstiloTarjeta,
            EstiloAviso = o.EstiloAviso
        });
    }

    private void PushCardCfg()
    {
        if (!IsPerController || SelectedController == null) return;
        _overlay.ApplyCardConfig(new OverlayCardConfig
        {
            ControllerId = SelectedController.Id,
            ColorAcento = Accent,
            ColorFondo = Bg,
            EstiloTarjeta = EstilosTarjeta[EstiloTarjetaIdx],
            VerBateria = ShowBattery,
            VerLatencia = ShowLatency,
            VerFrecuencia = ShowPolling,
            VerPerfil = ShowProfile,
            VerConexion = ShowConnection,
            VerTipo = ShowType,
            VerSenal = ShowSignal,
            AnclajeHud = Anchors[HudAnchorIdx],
            DesvioX = HudOffX,
            DesvioY = HudOffY,
            Escala = Scale
        });
    }

    private void PushAllCardCfg()
    {
        var count = _settings.Settings.Overlay.PorMando.Count;
        Log.Information("[Overlay] PushAllCardCfg: {Count} cards", count);
        foreach (var kv in _settings.Settings.Overlay.PorMando)
        {
            Log.Information("[Overlay]   Card {Id}: VerBateria={VB}, Accent={AC}, Offset=({DX},{DY})",
                kv.Key, kv.Value.VerBateria, kv.Value.ColorAcento, kv.Value.DesvioX, kv.Value.DesvioY);
            _overlay.ApplyCardConfig(kv.Value);
        }
    }

    private void PushHud()
    {
        try
        {
            var list = _controllers.ConnectedControllers
                .Where(c => IsMandoVisible(c))
                .Select(c =>
                {
                    var state = _controllers.GetCurrentState(c.Id);
                    if (state != null)
                        state = _inputProcessing.ProcessRawInput(state, c.AssignedProfileId ?? "");
                    return new InfoMando
                    {
                        Id = c.Id,
                        Nombre = c.EffectiveName,
                        NivelBateria = c.BatteryLevel,
                        Cargando = c.IsCharging,
                        Conectado = c.IsConnected,
                        LatenciaMs = c.LatencyMs,
                        Hz = c.PollingRateHz,
                        PerfilActivo = string.IsNullOrEmpty(c.AssignedProfileId) ? null : c.AssignedProfileId,
                        TipoMando = c.Type.ToString(),
                        Lx = state?.LeftStickX ?? 0,
                        Ly = state?.LeftStickY ?? 0,
                        Rx = state?.RightStickX ?? 0,
                        Ry = state?.RightStickY ?? 0,
                        L2 = state?.LeftTrigger ?? 0,
                        R2 = state?.RightTrigger ?? 0,
                        BtnA = state?.A ?? false,
                        BtnB = state?.B ?? false,
                        BtnX = state?.X ?? false,
                        BtnY = state?.Y ?? false,
                        BtnUp = state?.DPadUp ?? false,
                        BtnDown = state?.DPadDown ?? false,
                        BtnLeft = state?.DPadLeft ?? false,
                        BtnRight = state?.DPadRight ?? false,
                        BtnLB = state?.LeftBumper ?? false,
                        BtnRB = state?.RightBumper ?? false
                    };
                }).ToList();
            Log.Information("[Overlay] PushHud: Sending {Count} controllers", list.Count);
            foreach (var m in list)
                Log.Information("[Overlay]   Controller {Id}: {Name}, Bat={NivelBateria}%, Con={Conectado}", m.Id, m.Nombre, m.NivelBateria, m.Conectado);
            _overlay.UpdateHud(new InformeMandos { Lista = list });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[Overlay] PushHud failed");
        }
    }

    private static bool IsMandoVisible(ControllerInfo c) =>
        !c.IsEmulated
        && !string.IsNullOrEmpty(c.EffectiveName)
        && c.EffectiveName != "Unknown Controller"
        && c.Type != Core.Enums.ControllerType.Unknown;
}
