using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Events;
using NovaPad.Core.Interfaces;
using NovaPad.WPF.Services;

namespace NovaPad.WPF.ViewModels;

public partial class OverlaySettingsViewModel : ObservableObject
{
    private readonly IAppSettingsService _settings;
    private readonly OverlayProcessManager _overlayManager;
    private readonly OverlayClient _overlayClient;
    private readonly IControllerManagerService _controllerManager;

    [ObservableProperty] private bool _autoStartOverlay = true;
    [ObservableProperty] private double _opacity = 0.8;
    [ObservableProperty] private bool _showBattery = true;
    [ObservableProperty] private bool _showLatency = true;
    [ObservableProperty] private bool _showPollingRate;
    [ObservableProperty] private bool _showProfileName = true;
    [ObservableProperty] private bool _showConnectionStatus = true;
    [ObservableProperty] private bool _showFps;
    [ObservableProperty] private bool _showControllerType = true;
    [ObservableProperty] private bool _showNotifications = true;
    [ObservableProperty] private string _overlayStatusText = "Detenido";
    [ObservableProperty] private bool _canStart = true;
    [ObservableProperty] private bool _canStop;

    // Position
    [ObservableProperty] private int _hudAnchorIndex;
    [ObservableProperty] private double _hudOffsetX;
    [ObservableProperty] private double _hudOffsetY;
    [ObservableProperty] private int _notificationAnchorIndex = 6; // BottomRight
    [ObservableProperty] private double _notificationOffsetX = 24;
    [ObservableProperty] private double _notificationOffsetY = 80;

    public string[] AnchorOptions { get; } =
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

    public OverlaySettingsViewModel(IAppSettingsService settings, OverlayProcessManager overlayManager, OverlayClient overlayClient, IControllerManagerService controllerManager)
    {
        _settings = settings;
        _overlayManager = overlayManager;
        _overlayClient = overlayClient;
        _controllerManager = controllerManager;

        LoadSettings();
        UpdateOverlayStatus();
    }

    private void LoadSettings()
    {
        var s = _settings.Settings;
        AutoStartOverlay = s.AutoStartOverlay;
        Opacity = s.Overlay.Opacity;
        ShowBattery = s.Overlay.ShowBattery;
        ShowLatency = s.Overlay.ShowLatency;
        ShowPollingRate = s.Overlay.ShowPollingRate;
        ShowProfileName = s.Overlay.ShowProfileName;
        ShowConnectionStatus = s.Overlay.ShowConnectionStatus;
        ShowFps = s.Overlay.ShowFps;
        ShowControllerType = s.Overlay.ShowControllerType;
        HudOffsetX = s.Overlay.X;
        HudOffsetY = s.Overlay.Y;
        var idx = Array.IndexOf(AnchorOptions, s.Overlay.Anchor);
        HudAnchorIndex = idx >= 0 ? idx : 0;

        ShowNotifications = s.Overlay.ShowNotifications;
        var nidx = Array.IndexOf(AnchorOptions, s.Overlay.NotificationAnchor);
        NotificationAnchorIndex = nidx >= 0 ? nidx : 6;
        NotificationOffsetX = s.Overlay.NotificationOffsetX;
        NotificationOffsetY = s.Overlay.NotificationOffsetY;
    }

    private void UpdateOverlayStatus()
    {
        if (_overlayManager.IsRunning)
        {
            OverlayStatusText = "En ejecución";
            CanStart = false;
            CanStop = true;
        }
        else
        {
            OverlayStatusText = "Detenido";
            CanStart = true;
            CanStop = false;
        }
    }

    partial void OnAutoStartOverlayChanged(bool value) => SaveToDisk();
    partial void OnOpacityChanged(double value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnShowBatteryChanged(bool value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnShowLatencyChanged(bool value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnShowPollingRateChanged(bool value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnShowProfileNameChanged(bool value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnShowConnectionStatusChanged(bool value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnShowFpsChanged(bool value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnShowControllerTypeChanged(bool value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnShowNotificationsChanged(bool value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnHudAnchorIndexChanged(int value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnHudOffsetXChanged(double value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnHudOffsetYChanged(double value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnNotificationAnchorIndexChanged(int value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnNotificationOffsetXChanged(double value) { SaveToDisk(); _ = SendConfigAsync(); }
    partial void OnNotificationOffsetYChanged(double value) { SaveToDisk(); _ = SendConfigAsync(); }

    [RelayCommand]
    private async Task StartOverlay()
    {
        await _overlayManager.StartAsync();
        UpdateOverlayStatus();
        await SendConfigAsync();
        await SendHudUpdateAsync();
    }

    [RelayCommand]
    private void StopOverlay()
    {
        _overlayManager.Stop();
        UpdateOverlayStatus();
    }

    private void SaveToDisk()
    {
        var s = _settings.Settings;
        s.AutoStartOverlay = AutoStartOverlay;
        s.Overlay.Opacity = Opacity;
        s.Overlay.ShowBattery = ShowBattery;
        s.Overlay.ShowLatency = ShowLatency;
        s.Overlay.ShowPollingRate = ShowPollingRate;
        s.Overlay.ShowProfileName = ShowProfileName;
        s.Overlay.ShowConnectionStatus = ShowConnectionStatus;
        s.Overlay.ShowFps = ShowFps;
        s.Overlay.ShowControllerType = ShowControllerType;
        s.Overlay.X = HudOffsetX;
        s.Overlay.Y = HudOffsetY;
        s.Overlay.Anchor = AnchorOptions[HudAnchorIndex];
        s.Overlay.ShowNotifications = ShowNotifications;
        s.Overlay.NotificationAnchor = AnchorOptions[NotificationAnchorIndex];
        s.Overlay.NotificationOffsetX = NotificationOffsetX;
        s.Overlay.NotificationOffsetY = NotificationOffsetY;
        _settings.Save();
    }

    private async Task SendConfigAsync()
    {
        await _overlayClient.SendAsync(new OverlayConfigEvent
        {
            Opacity = Opacity,
            ShowBattery = ShowBattery,
            ShowLatency = ShowLatency,
            ShowPollingRate = ShowPollingRate,
            ShowProfileName = ShowProfileName,
            ShowConnectionStatus = ShowConnectionStatus,
            ShowFps = ShowFps,
            ShowControllerType = ShowControllerType,
            ShowNotifications = ShowNotifications,
            HudAnchor = AnchorOptions[HudAnchorIndex],
            HudOffsetX = HudOffsetX,
            HudOffsetY = HudOffsetY,
            NotificationAnchor = AnchorOptions[NotificationAnchorIndex],
            NotificationOffsetX = NotificationOffsetX,
            NotificationOffsetY = NotificationOffsetY
        });
    }

    private async Task SendHudUpdateAsync()
    {
        try
        {
            var controllers = _controllerManager.ConnectedControllers
                .Select(c => new HudControllerData
                {
                    DeviceId = c.Id,
                    Name = c.EffectiveName,
                    BatteryLevel = c.BatteryLevel,
                    IsCharging = c.IsCharging,
                    IsConnected = c.IsConnected,
                    LatencyMs = c.LatencyMs,
                    PollingRateHz = c.PollingRateHz,
                    ProfileName = string.IsNullOrEmpty(c.AssignedProfileId) ? null : c.AssignedProfileId
                }).ToList();

            await _overlayClient.SendAsync(new HudUpdateEvent { Controllers = controllers });
        }
        catch { }
    }
}
