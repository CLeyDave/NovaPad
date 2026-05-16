using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllerManager;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private int _connectedControllers;

    [ObservableProperty]
    private int _totalControllers;

    [ObservableProperty]
    private string _averageLatency = "0 ms";

    [ObservableProperty]
    private int _totalBatteryAverage;

    [ObservableProperty]
    private string _uptime = "0h 0m";

    public ObservableCollection<DashboardControllerItem> ControllerCards { get; } = new();

    private readonly DateTime _startTime = DateTime.Now;

    public DashboardViewModel(IControllerManagerService controllerManager, INavigationService navigation)
    {
        _controllerManager = controllerManager;
        _navigation = navigation;
        _controllerManager.ControllerUpdated += OnControllerChanged;
        _controllerManager.ControllerConnected += OnControllerChanged;
        _controllerManager.ControllerDisconnected += OnControllerChanged;
        RefreshData();
    }

    private void OnControllerChanged(object? sender, ControllerInfo e)
    {
        App.Current?.Dispatcher.Invoke(RefreshData);
    }

    private void RefreshData()
    {
        var controllers = _controllerManager.ConnectedControllers
            .Where(c => !c.IsEmulated && !string.IsNullOrEmpty(c.EffectiveName) && c.EffectiveName != "Unknown Controller" && c.Type != Core.Enums.ControllerType.Unknown).ToList();
        TotalControllers = controllers.Count;
        ConnectedControllers = controllers.Count(c => c.IsConnected);

        if (controllers.Count > 0)
        {
            AverageLatency = $"{controllers.Average(c => c.LatencyMs):F1} ms";
            TotalBatteryAverage = (int)controllers.Where(c => c.BatteryLevel >= 0)
                .DefaultIfEmpty().Average(c => c?.BatteryLevel ?? 0);
        }

        var elapsed = DateTime.Now - _startTime;
        Uptime = $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";

        ControllerCards.Clear();
        foreach (var ctrl in controllers)
        {
            var uptime = DateTime.UtcNow - ctrl.FirstSeen;
            ControllerCards.Add(new DashboardControllerItem
            {
                Id = ctrl.Id,
                Name = ctrl.EffectiveName,
                Type = ctrl.Type.ToString(),
                BatteryLevel = ctrl.BatteryLevel,
                IsCharging = ctrl.IsCharging,
                Latency = $"{ctrl.LatencyMs:F1} ms",
                Manufacturer = ctrl.Manufacturer,
                SerialNumber = string.IsNullOrEmpty(ctrl.SerialNumber) ? "N/A" : ctrl.SerialNumber,
                FirmwareVersion = string.IsNullOrEmpty(ctrl.FirmwareVersion) ? "N/A" : ctrl.FirmwareVersion,
                PollingRate = $"{ctrl.PollingRateHz} Hz",
                ConnectionType = ctrl.Connection.ToString(),
                UptimeText = $"{(int)uptime.TotalHours}h {uptime.Minutes}m",
                HasTouchpad = ctrl.HasTouchpad,
                HasGyroscope = ctrl.HasGyroscope,
                HasRumble = ctrl.HasRumble,
                HasBattery = ctrl.HasBattery,
                ConnectionIcon = ctrl.Connection switch
                {
                    Core.Enums.ConnectionType.Bluetooth => (Geometry)Application.Current.FindResource("BluetoothIcon"),
                    Core.Enums.ConnectionType.Usb => (Geometry)Application.Current.FindResource("UsbIcon"),
                    _ => (Geometry)Application.Current.FindResource("UsbIcon")
                },
                IsConnected = ctrl.IsConnected
            });
        }
    }

    [RelayCommand]
    private void NavigateToDetail(DashboardControllerItem? item)
    {
        if (item == null) return;
        ControllerDetailViewModel.PendingControllerId = item.Id;
        _navigation.NavigateTo("ControllerDetailView");
    }
}

public class DashboardControllerItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int BatteryLevel { get; set; }
    public bool IsCharging { get; set; }
    public string Latency { get; set; } = "0 ms";
    public Geometry? ConnectionIcon { get; set; }
    public bool IsConnected { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = "N/A";
    public string FirmwareVersion { get; set; } = "N/A";
    public string PollingRate { get; set; } = "0 Hz";
    public string ConnectionType { get; set; } = string.Empty;
    public string UptimeText { get; set; } = "0h 0m";
    public bool HasTouchpad { get; set; }
    public bool HasGyroscope { get; set; }
    public bool HasRumble { get; set; }
    public bool HasBattery { get; set; }
}
