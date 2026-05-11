using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Enums;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.ViewModels;

public partial class ControllerDetailViewModel : ObservableObject
{
    private readonly IControllerManagerService _controllerManager;
    private readonly IInputProcessingService _inputProcessor;
    private readonly ILightingService? _lighting;
    private readonly DispatcherTimer _pollingTimer;

    [ObservableProperty]
    private double _leftStickX;

    [ObservableProperty]
    private double _leftStickY;

    [ObservableProperty]
    private double _rightStickX;

    [ObservableProperty]
    private double _rightStickY;

    [ObservableProperty]
    private double _leftTrigger;

    [ObservableProperty]
    private double _rightTrigger;

    [ObservableProperty]
    private double _leftStickDeadzone = 0.15;

    [ObservableProperty]
    private double _rightStickDeadzone = 0.15;

    [ObservableProperty]
    private double _triggerDeadzone = 0.1;

    [ObservableProperty]
    private string _leftCurve = "Linear";

    [ObservableProperty]
    private string _rightCurve = "Linear";

    [ObservableProperty]
    private bool _isA;

    [ObservableProperty]
    private bool _isB;

    [ObservableProperty]
    private bool _isX;

    [ObservableProperty]
    private bool _isY;

    [ObservableProperty]
    private bool _isDpadUp;

    [ObservableProperty]
    private bool _isDpadDown;

    [ObservableProperty]
    private bool _isDpadLeft;

    [ObservableProperty]
    private bool _isDpadRight;

    [ObservableProperty]
    private bool _isLB;

    [ObservableProperty]
    private bool _isRB;

    [ObservableProperty]
    private bool _isStart;

    [ObservableProperty]
    private bool _isBack;

    [ObservableProperty]
    private bool _isGuide;

    [ObservableProperty]
    private bool _isLSClick;

    [ObservableProperty]
    private bool _isRSClick;

    [ObservableProperty]
    private double _leftStickMagnitude;

    [ObservableProperty]
    private double _rightStickMagnitude;

    [ObservableProperty]
    private ControllerInfo? _selectedController;

    [ObservableProperty]
    private string _latency = "--";

    [ObservableProperty]
    private string _pollingRate = "--";

    [ObservableProperty]
    private bool _isRenaming;

    [ObservableProperty]
    private string _renameText = string.Empty;

    [ObservableProperty]
    private bool _isRgbSupported;

    [ObservableProperty]
    private byte _rgbRed = 255;

    [ObservableProperty]
    private byte _rgbGreen;

    [ObservableProperty]
    private byte _rgbBlue;

    [ObservableProperty]
    private int _selectedEffectIndex;

    public ObservableCollection<ControllerInfo> ConnectedControllers { get; } = new();

    public ObservableCollection<string> CurveOptions { get; } = new()
    {
        "Linear", "Exponential", "Logarithmic", "Aggressive"
    };

    public ControllerDetailViewModel(
        IControllerManagerService controllerManager,
        IInputProcessingService inputProcessor,
        ILightingService? lighting = null)
    {
        _controllerManager = controllerManager;
        _inputProcessor = inputProcessor;
        _lighting = lighting;
        _controllerManager.InputReceived += OnInputReceived;
        _controllerManager.ControllerConnected += OnControllerListChanged;
        _controllerManager.ControllerDisconnected += OnControllerListChanged;
        _controllerManager.ControllerUpdated += OnControllerUpdated;

        _pollingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollingTimer.Tick += (_, _) => RefreshControllerList();
        _pollingTimer.Start();

        RefreshControllerList();
    }

    partial void OnSelectedControllerChanged(ControllerInfo? value)
    {
        if (value != null)
            IsRgbSupported = _lighting?.IsSupported(value.Id) ?? false;
    }

    partial void OnLeftStickDeadzoneChanged(double value)
    {
        if (SelectedController != null)
            _inputProcessor.SetDeadZone(SelectedController.Id, value, RightStickDeadzone, TriggerDeadzone, TriggerDeadzone);
    }

    partial void OnRightStickDeadzoneChanged(double value)
    {
        if (SelectedController != null)
            _inputProcessor.SetDeadZone(SelectedController.Id, LeftStickDeadzone, value, TriggerDeadzone, TriggerDeadzone);
    }

    partial void OnTriggerDeadzoneChanged(double value)
    {
        if (SelectedController != null)
            _inputProcessor.SetDeadZone(SelectedController.Id, LeftStickDeadzone, RightStickDeadzone, value, value);
    }

    partial void OnLeftCurveChanged(string value)
    {
        if (SelectedController != null)
            _inputProcessor.SetStickCurve(SelectedController.Id, value, RightCurve);
    }

    partial void OnRightCurveChanged(string value)
    {
        if (SelectedController != null)
            _inputProcessor.SetStickCurve(SelectedController.Id, LeftCurve, value);
    }

    private void OnControllerUpdated(object? sender, ControllerInfo e)
    {
        if (SelectedController != null && e.Id == SelectedController.Id)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                PollingRate = $"{e.PollingRateHz} Hz";
                Latency = $"{e.LatencyMs:F1} ms";
            });
        }
    }

    private void OnControllerListChanged(object? sender, ControllerInfo e)
    {
        RefreshControllerList();
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

        var toSelect = !string.IsNullOrEmpty(previousId)
            ? ConnectedControllers.FirstOrDefault(c => c.Id == previousId)
            : ConnectedControllers.FirstOrDefault();

        if (toSelect != null)
            SelectedController = toSelect;
    }

    private void OnInputReceived(object? sender, ControllerState state)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (SelectedController == null || state.ControllerId != SelectedController.Id)
                return;

            LeftStickX = state.LeftStickX;
            LeftStickY = -state.LeftStickY;
            RightStickX = state.RightStickX;
            RightStickY = -state.RightStickY;
            LeftTrigger = state.LeftTrigger;
            RightTrigger = state.RightTrigger;

            IsA = state.A;
            IsB = state.B;
            IsX = state.X;
            IsY = state.Y;
            IsDpadUp = state.DPadUp;
            IsDpadDown = state.DPadDown;
            IsDpadLeft = state.DPadLeft;
            IsDpadRight = state.DPadRight;
            IsLB = state.LeftBumper;
            IsRB = state.RightBumper;
            IsStart = state.Start;
            IsBack = state.Back;
            IsGuide = state.Guide;
            IsLSClick = state.LeftStickClick;
            IsRSClick = state.RightStickClick;

            LeftStickMagnitude = Math.Sqrt(state.LeftStickX * state.LeftStickX + state.LeftStickY * state.LeftStickY);
            RightStickMagnitude = Math.Sqrt(state.RightStickX * state.RightStickX + state.RightStickY * state.RightStickY);
        });
    }

    [RelayCommand]
    private void StartRename()
    {
        if (SelectedController == null) return;
        RenameText = SelectedController.EffectiveName;
        IsRenaming = true;
    }

    [RelayCommand]
    private async Task SaveRename()
    {
        if (SelectedController == null || string.IsNullOrWhiteSpace(RenameText)) return;
        await _controllerManager.RenameControllerAsync(SelectedController.Id, RenameText.Trim());
        SelectedController.CustomName = RenameText.Trim();
        IsRenaming = false;
    }

    [RelayCommand]
    private void CancelRename()
    {
        IsRenaming = false;
    }

    [RelayCommand]
    private void RefreshController()
    {
        RefreshControllerList();
    }

    [RelayCommand]
    private void ApplyRgb()
    {
        if (_lighting == null || SelectedController == null) return;
        var effect = (LightEffect)SelectedEffectIndex;
        _lighting.SetEffect(SelectedController.Id, effect, RgbRed, RgbGreen, RgbBlue);
    }

    [RelayCommand]
    private void StopRgb()
    {
        if (_lighting == null || SelectedController == null) return;
        _lighting.Stop(SelectedController.Id);
    }
}
