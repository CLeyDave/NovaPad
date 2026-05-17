using System.Collections.ObjectModel;
using System.Reflection;
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
    private readonly IOverlayService _overlay;
    private readonly AdminOverlayVm _overlayVm;
    private readonly DispatcherTimer _batteryTimer;
    private readonly DispatcherTimer _notificationCleanupTimer;
    private bool _isScanning;

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

    public MainViewModel(INavigationService navigation, IThemeService themeService, IAppSettingsService settings, IControllerManagerService controllerManager, INotificationService notificationService, IOverlayService overlay, AdminOverlayVm overlayVm)
    {
        _navigation = navigation;
        _themeService = themeService;
        _settings = settings;
        _controllerManager = controllerManager;
        _notificationService = notificationService;
        _overlay = overlay;
        _overlayVm = overlayVm;

        _isSidebarExpanded = _settings.Settings.Window.SidebarExpanded;
        _isDarkTheme = _themeService.IsDarkTheme;

        _controllerManager.ControllerConnected += OnControllerConnected;
        _controllerManager.ControllerDisconnected += OnControllerDisconnected;
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

        _notificationService.NotificationReceived += (_, notification) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => Notifications.Add(notification));
        };

        var loc = LocalizationService.Instance;
        NavigationItems.Add(new NavigationItem { Label = loc["Nav_Dashboard"], Icon = G("HomeIcon"), ViewName = nameof(DashboardView) });
        NavigationItems.Add(new NavigationItem { Label = loc["Nav_Controllers"], Icon = G("GamepadIcon"), ViewName = nameof(ControllerListView) });
        NavigationItems.Add(new NavigationItem { Label = loc["Nav_Details"], Icon = G("InfoIcon"), ViewName = nameof(ControllerDetailView) });
        NavigationItems.Add(new NavigationItem { Label = loc["Nav_DeviceInfo"], Icon = G("ActivityIcon"), ViewName = nameof(DeviceInfoView) });
        NavigationItems.Add(new NavigationItem { Label = loc["Nav_Battery"], Icon = G("BatteryFullIcon"), ViewName = nameof(BatteryView) });
        NavigationItems.Add(new NavigationItem { Label = loc["Nav_RGB"], Icon = G("PaletteIcon"), ViewName = nameof(RGBView) });
        NavigationItems.Add(new NavigationItem { Label = loc["Nav_Overlay"], Icon = G("LayersIcon"), ViewName = "OverlaySettingsView" });
        NavigationItems.Add(new NavigationItem { Label = loc["Nav_Settings"], Icon = G("SettingsIcon"), ViewName = nameof(SettingsView) });

        SelectedItem = NavigationItems[0];

        LocalizationService.Instance.PropertyChanged += (_, _) => UpdateNavLabels();
    }

    private static readonly Dictionary<string, string> _navKeyMap = new()
    {
        ["DashboardView"] = "Nav_Dashboard",
        ["ControllerListView"] = "Nav_Controllers",
        ["ControllerDetailView"] = "Nav_Details",
        ["DeviceInfoView"] = "Nav_DeviceInfo",
        ["BatteryView"] = "Nav_Battery",
        ["RGBView"] = "Nav_RGB",
        ["OverlaySettingsView"] = "Nav_Overlay",
        ["SettingsView"] = "Nav_Settings",
    };

    private void UpdateNavLabels()
    {
        var loc = LocalizationService.Instance;
        foreach (var item in NavigationItems)
        {
            if (_navKeyMap.TryGetValue(item.ViewName, out var key))
                item.Label = loc[key];
        }
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

    public AdminOverlayVm OverlayVm => _overlayVm;

    public string AppVersion
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v != null ? $"v{v.Major}.{v.Minor}.{v.Build}" : "v1.0.0";
        }
    }

    [RelayCommand]
    private async Task ScanControllers()
    {
        if (_isScanning) return;
        _isScanning = true;
        try { await _controllerManager.ScanForControllersAsync(); }
        finally { _isScanning = false; }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        IsDarkTheme = _themeService.IsDarkTheme;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigation.GoBack();
        SyncSelectedItem();
    }

    [RelayCommand]
    private void GoForward()
    {
        _navigation.GoForward();
        SyncSelectedItem();
    }

    private void SyncSelectedItem()
    {
        var name = _navigation.CurrentViewName;
        SelectedItem = NavigationItems.FirstOrDefault(n => n.ViewName == name);
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
            && (DateTime.UtcNow - n.Timestamp).TotalSeconds < 4);
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

            _overlay.NotifyConnection(new EstadoConexion
            {
                Conectado = true,
                IdMando = e.Id,
                NombreMando = e.EffectiveName,
                Bateria = e.BatteryLevel,
                Cargando = e.IsCharging
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

            _overlay.NotifyConnection(new EstadoConexion
            {
                Conectado = false,
                IdMando = e.Id,
                NombreMando = e.EffectiveName
            });
        });
    }

    private void CleanupNotifications()
    {
        var maxAge = _settings.Settings.Notifications.DurationSeconds + 2;
        var expired = Notifications.Where(n => (DateTime.UtcNow - n.Timestamp).TotalSeconds > maxAge).ToList();
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

        SendHudUpdateAsync(visible);
    }

    private void SendHudUpdateAsync(List<ControllerInfo> visible)
    {
        try
        {
            var hudEvent = new InformeMandos
            {
                Lista = visible.Select(c => new InfoMando
                {
                    Id = c.Id,
                    Nombre = c.EffectiveName,
                    NivelBateria = c.BatteryLevel,
                    Cargando = c.IsCharging,
                    Conectado = c.IsConnected,
                    LatenciaMs = c.LatencyMs,
                    Hz = c.PollingRateHz,
                    PerfilActivo = string.IsNullOrEmpty(c.AssignedProfileId) ? null : c.AssignedProfileId,
                    TipoMando = c.Type.ToString()
                }).ToList()
            };
            _overlay.UpdateHud(hudEvent);
        }
        catch { }
    }
}

public partial class NavigationItem : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;
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
        set
        {
            if (SetProperty(ref _isCharging, value))
                OnPropertyChanged(nameof(BatteryIconKey));
        }
    }

    public string BatteryText => Level >= 0 ? $"{Level}%" : "--";
    public string BatteryIconKey => IsCharging ? "BatteryChargingIcon" : "BatteryFullIcon";
}
