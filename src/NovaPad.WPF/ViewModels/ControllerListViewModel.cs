using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.ViewModels;

public partial class ControllerListViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllerManager;

    [ObservableProperty]
    private ControllerItem? _selectedController;

    public ObservableCollection<ControllerItem> Controllers { get; } = new();

    public ControllerListViewModel(IControllerManagerService controllerManager)
    {
        _controllerManager = controllerManager;
        _controllerManager.ControllerUpdated += OnControllerUpdated;
        _controllerManager.ControllerConnected += OnControllerConnected;
        _controllerManager.ControllerDisconnected += OnControllerDisconnected;

        RefreshList();
    }

    private void OnControllerUpdated(object? sender, ControllerInfo e) => RefreshList();
    private void OnControllerConnected(object? sender, ControllerInfo e) => RefreshList();
    private void OnControllerDisconnected(object? sender, ControllerInfo e) => RefreshList();

    private void RefreshList()
    {
        Controllers.Clear();
        foreach (var ctrl in _controllerManager.ConnectedControllers
            .Where(c => !c.IsEmulated && !string.IsNullOrEmpty(c.EffectiveName) && c.EffectiveName != "Unknown Controller" && c.Type != Core.Enums.ControllerType.Unknown))
        {
            Controllers.Add(new ControllerItem
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
            });
        }
    }

    [RelayCommand]
    private void SelectController(ControllerItem? item)
    {
        SelectedController = item;
    }

    [RelayCommand]
    private async Task DisconnectController(ControllerItem? item)
    {
        if (item == null) return;
        await _controllerManager.DisconnectControllerAsync(item.Id);
        RefreshList();
    }

    [RelayCommand]
    private async Task RenameController(ControllerItem? item)
    {
        if (item == null) return;
        await _controllerManager.RenameControllerAsync(item.Id, item.Name);
        RefreshList();
    }

    [RelayCommand]
    private async Task RefreshDevices()
    {
        await _controllerManager.ScanForControllersAsync();
        RefreshList();
    }
}

public class ControllerItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = string.Empty;
    public int BatteryLevel { get; set; } = -1;
    public bool IsCharging { get; set; }
    public bool HasBattery { get; set; }
    public string Latency { get; set; } = "0 ms";
    public double SignalStrength { get; set; }
    public string PollingRate { get; set; } = "0 Hz";
    public bool IsConnected { get; set; } = true;
    public bool HasTouchpad { get; set; }
    public bool HasGyroscope { get; set; }
}
