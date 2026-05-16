using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;

namespace NovaPad.WPF.Services;

public class InputProcessingService : IInputProcessingService
{
    public event EventHandler<ProcessedInputEventArgs>? InputProcessed;

    private readonly IAppSettingsService _settings;

    public InputProcessingService(IAppSettingsService settings)
    {
        _settings = settings;
    }

    public ControllerState ProcessRawInput(ControllerState rawState, string profileId)
    {
        var cfg = ObtenerConfig(rawState.ControllerId);

        var processed = new ControllerState
        {
            ControllerId = rawState.ControllerId,
            Timestamp = rawState.Timestamp,
            A = rawState.A, B = rawState.B, X = rawState.X, Y = rawState.Y,
            DPadUp = rawState.DPadUp, DPadDown = rawState.DPadDown,
            DPadLeft = rawState.DPadLeft, DPadRight = rawState.DPadRight,
            LeftBumper = rawState.LeftBumper, RightBumper = rawState.RightBumper,
            Start = rawState.Start, Back = rawState.Back, Guide = rawState.Guide,
            Share = rawState.Share, Options = rawState.Options,
            LeftStickClick = rawState.LeftStickClick, RightStickClick = rawState.RightStickClick,
            TouchpadClick = rawState.TouchpadClick, TouchpadTouch = rawState.TouchpadTouch,
            BatteryLevel = rawState.BatteryLevel, IsCharging = rawState.IsCharging,
            LatencyMs = rawState.LatencyMs
        };

        processed.LeftStickX = ProcessStickAxis(
            rawState.LeftStickX, cfg.LeftStickDeadZone, cfg.LeftStickSensitivity,
            cfg.LeftStickCurve, cfg.InvertLeftX);

        processed.LeftStickY = ProcessStickAxis(
            rawState.LeftStickY, cfg.LeftStickDeadZone, cfg.LeftStickSensitivity,
            cfg.LeftStickCurve, cfg.InvertLeftY);

        processed.RightStickX = ProcessStickAxis(
            rawState.RightStickX, cfg.RightStickDeadZone, cfg.RightStickSensitivity,
            cfg.RightStickCurve, cfg.InvertRightX);

        processed.RightStickY = ProcessStickAxis(
            rawState.RightStickY, cfg.RightStickDeadZone, cfg.RightStickSensitivity,
            cfg.RightStickCurve, cfg.InvertRightY);

        processed.LeftTrigger = ProcessTriggerAxis(
            rawState.LeftTrigger, cfg.LeftTriggerDeadZone);

        processed.RightTrigger = ProcessTriggerAxis(
            rawState.RightTrigger, cfg.RightTriggerDeadZone);

        InputProcessed?.Invoke(this, new ProcessedInputEventArgs
        {
            ControllerId = rawState.ControllerId,
            ProcessedState = processed
        });

        return processed;
    }

    public void SetDeadZone(string controllerId, double leftStick, double rightStick, double leftTrigger, double rightTrigger)
    {
        var cfg = ObtenerConfig(controllerId);
        cfg.LeftStickDeadZone = leftStick;
        cfg.RightStickDeadZone = rightStick;
        cfg.LeftTriggerDeadZone = leftTrigger;
        cfg.RightTriggerDeadZone = rightTrigger;
        _settings.Save();
    }

    public void SetStickCurve(string controllerId, string leftCurve, string rightCurve)
    {
        var cfg = ObtenerConfig(controllerId);
        cfg.LeftStickCurve = leftCurve;
        cfg.RightStickCurve = rightCurve;
        _settings.Save();
    }

    public void SetInversion(string controllerId, bool invertLeftX, bool invertLeftY, bool invertRightX, bool invertRightY)
    {
        var cfg = ObtenerConfig(controllerId);
        cfg.InvertLeftX = invertLeftX;
        cfg.InvertLeftY = invertLeftY;
        cfg.InvertRightX = invertRightX;
        cfg.InvertRightY = invertRightY;
        _settings.Save();
    }

    private InputProcessingConfig ObtenerConfig(string controllerId)
    {
        var dict = _settings.Settings.InputProcessing;
        if (!dict.TryGetValue(controllerId, out var cfg))
        {
            cfg = new InputProcessingConfig();
            dict[controllerId] = cfg;
        }
        return cfg;
    }

    private static double ProcessStickAxis(double value, double deadZone, double sensitivity, string curve, bool invert)
    {
        if (invert) value = -value;

        var magnitude = Math.Abs(value);
        if (magnitude <= deadZone) return 0;

        var scaled = (magnitude - deadZone) / (1.0 - deadZone);
        scaled = ApplyCurve(scaled, curve);
        scaled *= sensitivity;

        return Math.Clamp(scaled * Math.Sign(value), -1.0, 1.0);
    }

    private static double ProcessTriggerAxis(double value, double deadZone)
    {
        if (value <= deadZone) return 0;
        return (value - deadZone) / (1.0 - deadZone);
    }

    private static double ApplyCurve(double value, string curve)
    {
        return curve switch
        {
            "Exponential" => value * value,
            "Logarithmic" => Math.Log(value + 1) / Math.Log(2),
            "Aggressive" => value * value * value + value * 0.3,
            _ => value
        };
    }
}
