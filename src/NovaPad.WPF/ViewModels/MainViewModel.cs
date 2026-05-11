using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Events;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;
using NovaPad.WPF.Services;
using NovaPad.WPF.Views;

namespace NovaPad.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IThemeService _themeService;
    private readonly IAppSettingsService _settings;
    private readonly IControllerManagerService _controllerManager;
    private readonly INotificationService _notificationService;
    private readonly OverlayProcessManager _overlayManager;
    private readonly DispatcherTimer _batteryTimer;
    private readonly DispatcherTimer _notificationCleanupTimer;

    public ObservableCollection<SidebarBatteryItem> BatteryItems { get; } = new();
    public ObservableCollection<NotificationMessage> Notifications { get; } = new();

    [ObservableProperty]
    private string _appTitle = "NovaPad";

    [ObservableProperty]
    private bool _isSidebarExpanded;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private NavigationItem? _selectedItem;

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

    public MainViewModel(INavigationService navigation, IThemeService themeService, IAppSettingsService settings, IControllerManagerService controllerManager, INotificationService notificationService, OverlayProcessManager overlayManager)
    {
        _navigation = navigation;
        _themeService = themeService;
        _settings = settings;
        _controllerManager = controllerManager;
        _notificationService = notificationService;
        _overlayManager = overlayManager;

        _isSidebarExpanded = _settings.Settings.Window.SidebarExpanded;
        _isDarkTheme = _themeService.IsDarkTheme;

        _controllerManager.ControllerConnected += OnControllerConnected;
        _controllerManager.ControllerDisconnected += OnControllerDisconnected;
        _controllerManager.ControllerUpdated += OnControllerListChanged;
        _controllerManager.InputReceived += OnInputReceived;

        _batteryTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _batteryTimer.Tick += (_, _) => RefreshBattery();
        _batteryTimer.Start();

        _notificationCleanupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(6)
        };
        _notificationCleanupTimer.Tick += (_, _) => CleanupNotifications();
        _notificationCleanupTimer.Start();

        RefreshBattery();

        NavigationItems.Add(new NavigationItem { Label = "Panel", Icon = G("HomeIcon"), ViewName = "DashboardView" });
        NavigationItems.Add(new NavigationItem { Label = "Mandos", Icon = G("GamepadIcon"), ViewName = "ControllerListView" });
        NavigationItems.Add(new NavigationItem { Label = "Detalles", Icon = G("InfoIcon"), ViewName = "ControllerDetailView" });
        NavigationItems.Add(new NavigationItem { Label = "Info Dispositivo", Icon = G("ActivityIcon"), ViewName = "DeviceInfoView" });
        NavigationItems.Add(new NavigationItem { Label = "Batería", Icon = G("BatteryFullIcon"), ViewName = "BatteryView" });
        NavigationItems.Add(new NavigationItem { Label = "RGB", Icon = G("PaletteIcon"), ViewName = "RGBView" });
        NavigationItems.Add(new NavigationItem { Label = "Overlay", Icon = G("LayersIcon"), ViewName = "OverlaySettingsView" });
        NavigationItems.Add(new NavigationItem { Label = "Configuración", Icon = G("SettingsIcon"), ViewName = "SettingsView" });

        SelectedItem = NavigationItems[0];
    }

    partial void OnIsSidebarExpandedChanged(bool value)
    {
        _settings.Settings.Window.SidebarExpanded = value;
        _settings.Save();
    }

    partial void OnSelectedItemChanged(NavigationItem? value)
    {
        if (value != null)
        {
            _navigation.NavigateTo(value.ViewName);
        }
    }

    [RelayCommand]
    private async Task ScanControllers()
    {
        await _controllerManager.ScanForControllersAsync();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        IsDarkTheme = _themeService.IsDarkTheme;
    }

    private static bool IsControllerVisible(ControllerInfo c) =>
        !c.IsEmulated
        && !string.IsNullOrEmpty(c.EffectiveName)
        && c.EffectiveName != "Unknown Controller"
        && c.Type != Core.Enums.ControllerType.Unknown;

    private bool HasRecentNotification(string controllerId, string title)
    {
        return Notifications.Any(n =>
            n.Title == title
            && n.Message.StartsWith(controllerId)
            && (DateTime.Now - n.Timestamp).TotalSeconds < 4);
    }

    private void OnControllerConnected(object? sender, ControllerInfo e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (!IsControllerVisible(e)) return;

            RefreshBattery();

            var batteryMsg = e.BatteryLevel >= 0
                ? $"{e.EffectiveName} — Batería: {e.BatteryLevel}%{(e.IsCharging ? " (cargando)" : "")}"
                : e.EffectiveName;
            var notification = new NotificationMessage
            {
                Title = "Mando Conectado",
                Message = batteryMsg,
                Type = NotificationType.DeviceConnected,
                Duration = TimeSpan.FromSeconds(5)
            };
            Notifications.Add(notification);

            _ = _overlayManager.Client.SendAsync(new DeviceConnectedEvent
            {
                DeviceId = e.Id,
                Name = e.EffectiveName,
                BatteryLevel = e.BatteryLevel,
                IsCharging = e.IsCharging
            });
        });
    }

    private void OnControllerDisconnected(object? sender, ControllerInfo e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (!IsControllerVisible(e)) return;

            RefreshBattery();

            var notification = new NotificationMessage
            {
                Title = "Mando Desconectado",
                Message = e.EffectiveName,
                Type = NotificationType.DeviceDisconnected,
                Duration = TimeSpan.FromSeconds(5)
            };
            Notifications.Add(notification);

            _ = _overlayManager.Client.SendAsync(new DeviceDisconnectedEvent
            {
                DeviceId = e.Id,
                Name = e.EffectiveName
            });
        });
    }

    private void OnControllerListChanged(object? sender, ControllerInfo e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(RefreshBattery);
    }

    private void CleanupNotifications()
    {
        var maxAge = _settings.Settings.Notifications.DurationSeconds + 2;
        var expired = Notifications.Where(n => (DateTime.Now - n.Timestamp).TotalSeconds > maxAge).ToList();
        foreach (var n in expired) Notifications.Remove(n);
    }

    private void OnInputReceived(object? sender, ControllerState state)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var item = BatteryItems.FirstOrDefault(b => b.Id == state.ControllerId);
            if (item != null && state.BatteryLevel >= 0)
            {
                item.Level = state.BatteryLevel;
                item.IsCharging = state.IsCharging;
            }
        });
    }

    private static Geometry G(string key) => (Geometry)System.Windows.Application.Current.FindResource(key);

    private void RefreshBattery()
    {
        var visible = _controllerManager.ConnectedControllers.Where(IsControllerVisible).ToList();
        var currentIds = visible.Select(c => c.Id).ToHashSet();
        var existingIds = BatteryItems.Select(b => b.Id).ToHashSet();

        foreach (var removed in existingIds.Except(currentIds).ToList())
        {
            var item = BatteryItems.FirstOrDefault(b => b.Id == removed);
            if (item != null) BatteryItems.Remove(item);
        }

        foreach (var ctrl in visible.Where(c => !existingIds.Contains(c.Id)))
        {
            BatteryItems.Add(new SidebarBatteryItem
            {
                Id = ctrl.Id,
                Name = ctrl.EffectiveName,
                Level = ctrl.BatteryLevel,
                IsCharging = ctrl.IsCharging,
                ControllerType = ctrl.Type,
                Icon = G("GamepadIcon")
            });
        }

        foreach (var ctrl in visible)
        {
            var item = BatteryItems.FirstOrDefault(b => b.Id == ctrl.Id);
            if (item != null)
            {
                item.Level = ctrl.BatteryLevel;
                item.IsCharging = ctrl.IsCharging;
            }
        }

        _ = SendHudUpdateAsync(visible);
    }

    private async Task SendHudUpdateAsync(List<ControllerInfo> visible)
    {
        try
        {
            var hudEvent = new HudUpdateEvent
            {
                Controllers = visible.Select(c => new HudControllerData
                {
                    DeviceId = c.Id,
                    Name = c.EffectiveName,
                    BatteryLevel = c.BatteryLevel,
                    IsCharging = c.IsCharging,
                    IsConnected = c.IsConnected,
                    LatencyMs = c.LatencyMs,
                    PollingRateHz = c.PollingRateHz,
                    ProfileName = string.IsNullOrEmpty(c.AssignedProfileId) ? null : c.AssignedProfileId
                }).ToList()
            };
            await _overlayManager.Client.SendAsync(hudEvent);
        }
        catch { }
    }
}

public class NavigationItem
{
    public string Label { get; set; } = string.Empty;
    public Geometry? Icon { get; set; }
    public string ViewName { get; set; } = string.Empty;
}

public class SidebarBatteryItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Core.Enums.ControllerType ControllerType { get; set; }

    public Geometry? Icon { get; set; }

    private int _level = -1;
    public int Level
    {
        get => _level;
        set
        {
            if (SetProperty(ref _level, value))
                OnPropertyChanged(nameof(BatteryText));
        }
    }

    private bool _isCharging;
    public bool IsCharging
    {
        get => _isCharging;
        set => SetProperty(ref _isCharging, value);
    }

    public string BatteryText => Level >= 0 ? $"{Level}%" : "--";
    public string BatteryIconKey => IsCharging ? "BatteryChargingIcon" : "BatteryFullIcon";
}
