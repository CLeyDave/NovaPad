using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.ViewModels;

public partial class RGBViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllerManager;
    private readonly ILightingService _lighting;

    [ObservableProperty] private byte _rgbRed = 255;
    [ObservableProperty] private byte _rgbGreen;
    [ObservableProperty] private byte _rgbBlue;
    [ObservableProperty] private int _selectedEffectIndex;
    [ObservableProperty] private ControllerInfo? _selectedController;
    [ObservableProperty] private bool _isRgbSupported;

    public ObservableCollection<ControllerInfo> Controllers { get; } = new();

    public RGBViewModel(IControllerManagerService controllerManager, ILightingService lighting)
    {
        _controllerManager = controllerManager;
        _lighting = lighting;

        _controllerManager.ControllerConnected += OnControllerListChanged;
        _controllerManager.ControllerDisconnected += OnControllerListChanged;

        Refresh();
    }

    partial void OnSelectedControllerChanged(ControllerInfo? value)
    {
        IsRgbSupported = value != null && _lighting.IsSupported(value.Id);
    }

    private void OnControllerListChanged(object? sender, ControllerInfo e)
    {
        App.Current.Dispatcher.Invoke(Refresh);
    }

    private void Refresh()
    {
        var previousId = SelectedController?.Id;
        Controllers.Clear();

        foreach (var ctrl in _controllerManager.ConnectedControllers
            .Where(c => !c.IsEmulated && !string.IsNullOrEmpty(c.EffectiveName) && c.EffectiveName != "Unknown Controller" && c.Type != Core.Enums.ControllerType.Unknown))
        {
            Controllers.Add(ctrl);
        }

        SelectedController = !string.IsNullOrEmpty(previousId)
            ? Controllers.FirstOrDefault(c => c.Id == previousId)
            : Controllers.FirstOrDefault();
    }

    [RelayCommand]
    private void ApplyRgb()
    {
        if (SelectedController == null) return;
        var effect = (LightEffect)SelectedEffectIndex;
        _lighting.SetEffect(SelectedController.Id, effect, RgbRed, RgbGreen, RgbBlue);
    }

    [RelayCommand]
    private void StopRgb()
    {
        if (SelectedController == null) return;
        _lighting.Stop(SelectedController.Id);
    }
}
