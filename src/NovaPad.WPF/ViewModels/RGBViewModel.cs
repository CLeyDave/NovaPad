using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;
using Serilog;

namespace NovaPad.WPF.ViewModels;

public partial class RGBViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllerManager;
    private readonly ILightingService _lighting;
    private readonly IAppSettingsService _settingsService;

    [ObservableProperty] private byte _rgbRed = 255;
    [ObservableProperty] private byte _rgbGreen;
    [ObservableProperty] private byte _rgbBlue;
    [ObservableProperty] private byte _rgbRed2;
    [ObservableProperty] private byte _rgbGreen2;
    [ObservableProperty] private byte _rgbBlue2;
    [ObservableProperty] private int _selectedEffectIndex;
    [ObservableProperty] private ControllerInfo? _selectedController;
    [ObservableProperty] private bool _isRgbSupported;
    [ObservableProperty] private bool _isSecondColorVisible;

    public ObservableCollection<ControllerInfo> Controllers { get; } = new();

    public static List<ColorPreset> FullColors { get; } = new()
    {
        new("Rojo",     255, 0,   0),
        new("Naranja",  255, 128, 0),
        new("Amarillo", 255, 255, 0),
        new("Verde",    0,   255, 0),
        new("Cyan",     0,   255, 255),
        new("Azul",     0,   0,   255),
        new("Morado",   128, 0,   255),
        new("Rosa",     255, 0,   128),
        new("Magenta",  255, 0,   255),
        new("Turquesa", 64,  224, 208),
        new("Oro",      255, 215, 0),
        new("Lima",     50,  205, 50),
        new("Vino",     139, 0,   0),
        new("Plata",    192, 192, 192),
        new("Marrón",   139, 69,  19),
        new("Blanco",   255, 255, 255),
    };

    public string RgbHex => $"#{RgbRed:X2}{RgbGreen:X2}{RgbBlue:X2}";
    public string RgbLabel => $"RGB({RgbRed}, {RgbGreen}, {RgbBlue})";
    public SolidColorBrush CurrentColorBrush => new(Color.FromRgb(RgbRed, RgbGreen, RgbBlue));
    public string RgbHex2 => $"#{RgbRed2:X2}{RgbGreen2:X2}{RgbBlue2:X2}";
    public string RgbLabel2 => $"RGB({RgbRed2}, {RgbGreen2}, {RgbBlue2})";
    public SolidColorBrush CurrentColorBrush2 => new(Color.FromRgb(RgbRed2, RgbGreen2, RgbBlue2));

    partial void OnRgbRedChanged(byte value) => OnRgbChanged();
    partial void OnRgbGreenChanged(byte value) => OnRgbChanged();
    partial void OnRgbBlueChanged(byte value) => OnRgbChanged();

    partial void OnRgbRed2Changed(byte value) => OnRgbChanged2();
    partial void OnRgbGreen2Changed(byte value) => OnRgbChanged2();
    partial void OnRgbBlue2Changed(byte value) => OnRgbChanged2();

    private void OnRgbChanged()
    {
        OnPropertyChanged(nameof(RgbHex));
        OnPropertyChanged(nameof(RgbLabel));
        OnPropertyChanged(nameof(CurrentColorBrush));
    }

    private void OnRgbChanged2()
    {
        OnPropertyChanged(nameof(RgbHex2));
        OnPropertyChanged(nameof(RgbLabel2));
        OnPropertyChanged(nameof(CurrentColorBrush2));
    }

    partial void OnSelectedEffectIndexChanged(int value)
    {
        IsSecondColorVisible = value == (int)LightEffect.Strobe || value == (int)LightEffect.Wave;
    }

    public RGBViewModel(IControllerManagerService controllerManager, ILightingService lighting, IAppSettingsService settingsService)
    {
        _controllerManager = controllerManager;
        _lighting = lighting;
        _settingsService = settingsService;

        _controllerManager.ControllerConnected += OnControllerListChanged;
        _controllerManager.ControllerDisconnected += OnControllerListChanged;

        Refresh();
    }

    partial void OnSelectedControllerChanged(ControllerInfo? value)
    {
        IsRgbSupported = value != null && _lighting.IsSupported(value.Id);
        Log.Information("[RGBViewModel] Selected controller changed to {Name} ({Id}), RGB supported={Supported}",
            value?.EffectiveName ?? "null", value?.Id ?? "null", IsRgbSupported);

        if (value != null)
        {
            var saved = _settingsService.Settings.RgbState.GetValueOrDefault(value.Id);
            if (saved != null)
            {
                RgbRed = saved.R;
                RgbGreen = saved.G;
                RgbBlue = saved.B;
                RgbRed2 = saved.R2;
                RgbGreen2 = saved.G2;
                RgbBlue2 = saved.B2;
                SelectedEffectIndex = saved.EffectIndex;
            }
        }
    }

    private void OnControllerListChanged(object? sender, ControllerInfo e)
    {
        App.Current?.Dispatcher.Invoke(Refresh);
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
    private void SetPresetColor(ColorPreset preset)
    {
        RgbRed = preset.R;
        RgbGreen = preset.G;
        RgbBlue = preset.B;
    }

    private void SaveCurrentRgbState()
    {
        if (SelectedController == null) return;
        _settingsService.Settings.RgbState[SelectedController.Id] = new RgbSavedState
        {
            R = RgbRed, G = RgbGreen, B = RgbBlue,
            R2 = RgbRed2, G2 = RgbGreen2, B2 = RgbBlue2,
            EffectIndex = SelectedEffectIndex
        };
        _settingsService.Save();
    }

    [RelayCommand]
    private void ApplyRgb()
    {
        if (SelectedController == null) return;
        var effect = (LightEffect)SelectedEffectIndex;
        Log.Information("[RGBViewModel] ApplyRgb: controller={Name} ({Id}), effect={Effect}, RGB({R},{G},{B}), RGB2({R2},{G2},{B2})",
            SelectedController.EffectiveName, SelectedController.Id, effect, RgbRed, RgbGreen, RgbBlue, RgbRed2, RgbGreen2, RgbBlue2);
        _lighting.SetEffect(SelectedController.Id, effect, RgbRed, RgbGreen, RgbBlue, RgbRed2, RgbGreen2, RgbBlue2);
        SaveCurrentRgbState();
    }

    [RelayCommand]
    private void StopRgb()
    {
        if (SelectedController == null) return;
        Log.Information("[RGBViewModel] StopRgb: controller={Name} ({Id})",
            SelectedController.EffectiveName, SelectedController.Id);
        _lighting.Stop(SelectedController.Id);
    }
}

public record ColorPreset(string Name, byte R, byte G, byte B);
