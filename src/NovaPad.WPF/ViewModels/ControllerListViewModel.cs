using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.ViewModels;

public partial class ControllerListViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllerManager;
    private bool _isRefreshing;

    public ObservableCollection<ControllerItem> Controllers { get; } = new();

    public ControllerListViewModel(IControllerManagerService controllerManager)
    {
        _controllerManager = controllerManager;
        _controllerManager.ControllerUpdated += OnControllerUpdated;
        _controllerManager.ControllerConnected += OnControllerConnected;
        _controllerManager.ControllerDisconnected += OnControllerDisconnected;

        CargarInicial();
    }

    private void CargarInicial()
    {
        Controllers.Clear();
        foreach (var ctrl in _controllerManager.ConnectedControllers
            .Where(c => !c.IsEmulated && !string.IsNullOrEmpty(c.EffectiveName) && c.EffectiveName != "Unknown Controller" && c.Type != Core.Enums.ControllerType.Unknown))
        {
            Controllers.Add(CrearItem(ctrl));
        }
    }

    private static ControllerItem CrearItem(ControllerInfo ctrl) => new()
    {
        Id = ctrl.Id,
        Name = ctrl.EffectiveName,
        Type = ctrl.Type.ToString(),
        ConnectionType = ctrl.Connection.ToString(),
        BatteryLevel = ctrl.BatteryLevel,
        IsCharging = ctrl.IsCharging,
        HasBattery = ctrl.HasBattery && ctrl.BatteryLevel >= 0,
        Latency = $"{ctrl.LatencyMs:F1} ms",
        SignalStrength = ctrl.SignalStrength,
        PollingRate = $"{ctrl.PollingRateHz} Hz",
        IsConnected = ctrl.IsConnected,
        HasTouchpad = ctrl.HasTouchpad,
        HasGyroscope = ctrl.HasGyroscope
    };

    private void OnControllerUpdated(object? sender, ControllerInfo ctrl)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            var item = Controllers.FirstOrDefault(c => c.Id == ctrl.Id);
            if (item == null) return;
            item.BatteryLevel = ctrl.BatteryLevel;
            item.IsCharging = ctrl.IsCharging;
            item.HasBattery = ctrl.HasBattery && ctrl.BatteryLevel >= 0;
            item.Latency = $"{ctrl.LatencyMs:F1} ms";
            item.SignalStrength = ctrl.SignalStrength;
            item.PollingRate = $"{ctrl.PollingRateHz} Hz";
            item.IsConnected = ctrl.IsConnected;
        });
    }

    private void OnControllerConnected(object? sender, ControllerInfo ctrl)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            if (Controllers.Any(c => c.Id == ctrl.Id)) return;
            if (ctrl.IsEmulated || string.IsNullOrEmpty(ctrl.EffectiveName) || ctrl.EffectiveName == "Unknown Controller" || ctrl.Type == Core.Enums.ControllerType.Unknown)
                return;
            Controllers.Add(CrearItem(ctrl));
        });
    }

    private void OnControllerDisconnected(object? sender, ControllerInfo ctrl)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            var item = Controllers.FirstOrDefault(c => c.Id == ctrl.Id);
            if (item != null)
                Controllers.Remove(item);
        });
    }

    [RelayCommand]
    private async Task RefreshDevices()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        try
        {
            await _controllerManager.ScanForControllersAsync();
            CargarInicial();
        }
        finally { _isRefreshing = false; }
    }

    [RelayCommand]
    private async Task DisconnectController(ControllerItem? item)
    {
        if (item == null || string.IsNullOrEmpty(item.Id)) return;
        await _controllerManager.DisconnectControllerAsync(item.Id);
    }

    [RelayCommand]
    private void RenameController(ControllerItem? item)
    {
        if (item == null) return;
        item.EditName = item.Name;
        item.IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveRenameController(ControllerItem? item)
    {
        if (item == null || string.IsNullOrEmpty(item.Id)) return;
        var newName = item.EditName?.Trim();
        if (string.IsNullOrEmpty(newName)) { item.IsEditing = false; return; }
        await _controllerManager.RenameControllerAsync(item.Id, newName);
        item.Name = newName;
        item.IsEditing = false;
    }

    [RelayCommand]
    private void CancelRenameController(ControllerItem? item)
    {
        if (item == null) return;
        item.IsEditing = false;
    }
}

public partial class ControllerItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Type { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = string.Empty;

    private int _batteryLevel = -1;
    public int BatteryLevel
    {
        get => _batteryLevel;
        set => SetProperty(ref _batteryLevel, value);
    }

    private bool _isCharging;
    public bool IsCharging
    {
        get => _isCharging;
        set => SetProperty(ref _isCharging, value);
    }

    private bool _hasBattery;
    public bool HasBattery
    {
        get => _hasBattery;
        set => SetProperty(ref _hasBattery, value);
    }

    private string _latency = "0 ms";
    public string Latency
    {
        get => _latency;
        set => SetProperty(ref _latency, value);
    }

    private double _signalStrength;
    public double SignalStrength
    {
        get => _signalStrength;
        set => SetProperty(ref _signalStrength, value);
    }

    private string _pollingRate = "0 Hz";
    public string PollingRate
    {
        get => _pollingRate;
        set => SetProperty(ref _pollingRate, value);
    }

    private bool _isConnected = true;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public bool HasTouchpad { get; set; }
    public bool HasGyroscope { get; set; }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
                OnPropertyChanged(nameof(IsNotEditing));
        }
    }

    public bool IsNotEditing => !_isEditing;

    private string _editName = string.Empty;
    public string EditName
    {
        get => _editName;
        set => SetProperty(ref _editName, value);
    }
}
