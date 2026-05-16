using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Enums;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.ViewModels;

public partial class ControllerDetailViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllerManager;

    public static string? PendingControllerId { get; set; }

    [ObservableProperty]
    private ControllerInfo? _selectedController;

    [ObservableProperty]
    private string _latency = "--";

    [ObservableProperty]
    private string _pollingRate = "--";

    [ObservableProperty]
    private bool _isInfoView = true;

    [ObservableProperty]
    private bool _isGamepadView2;

    partial void OnIsInfoViewChanged(bool value)
    {
        if (value) { IsGamepadView2 = false; }
    }

    partial void OnIsGamepadView2Changed(bool value)
    {
        if (value) { IsInfoView = false; }
    }

    public ObservableCollection<ControllerInfo> ConnectedControllers { get; } = new();

    public ControllerDetailViewModel(IControllerManagerService controllerManager)
    {
        _controllerManager = controllerManager;
        _controllerManager.ControllerConnected += OnControllerListChanged;
        _controllerManager.ControllerDisconnected += OnControllerListChanged;
        _controllerManager.ControllerUpdated += OnControllerUpdated;

        RefreshControllerList();
    }

    private void OnControllerUpdated(object? sender, ControllerInfo e)
    {
        if (SelectedController != null && e.Id == SelectedController.Id)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                PollingRate = $"{e.PollingRateHz} Hz";
                Latency = $"{e.LatencyMs:F1} ms";
            });
        }
    }

    private void OnControllerListChanged(object? sender, ControllerInfo e)
    {
        App.Current?.Dispatcher.Invoke(RefreshControllerList);
    }

    private void RefreshControllerList()
    {
        var visible = _controllerManager.ConnectedControllers
            .Where(c => !c.IsEmulated
                && !string.IsNullOrEmpty(c.EffectiveName)
                && c.EffectiveName != "Unknown Controller"
                && c.Type != ControllerType.Unknown)
            .ToList();

        var previousId = SelectedController?.Id;
        ConnectedControllers.Clear();

        foreach (var ctrl in visible)
            ConnectedControllers.Add(ctrl);

        var targetId = !string.IsNullOrEmpty(PendingControllerId) ? PendingControllerId : previousId;
        PendingControllerId = null;

        var toSelect = !string.IsNullOrEmpty(targetId)
            ? ConnectedControllers.FirstOrDefault(c => c.Id == targetId)
            : ConnectedControllers.FirstOrDefault();

        if (toSelect != null)
            SelectedController = toSelect;
    }

    [RelayCommand]
    private void RefreshController()
    {
        RefreshControllerList();
    }
}
