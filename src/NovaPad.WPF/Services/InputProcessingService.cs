using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;
using NovaPad.Core.Enums;

namespace NovaPad.WPF.Services;

public class InputProcessingService : IInputProcessingService
{
    public event EventHandler<ProcessedInputEventArgs>? InputProcessed;

    private readonly Dictionary<string, InputConfig> _configs = new();

    public ControllerState ProcessRawInput(ControllerState rawState, string profileId)
    {
        if (!_configs.TryGetValue(rawState.ControllerId, out var config))
        {
            config = new InputConfig();
            _configs[rawState.ControllerId] = config;
        }

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
            rawState.LeftStickX, config.LeftStickDeadZone, config.LeftStickSensitivity,
            config.LeftStickCurve, config.InvertLeftX);

        processed.LeftStickY = ProcessStickAxis(
            rawState.LeftStickY, config.LeftStickDeadZone, config.LeftStickSensitivity,
            config.LeftStickCurve, config.InvertLeftY);

        processed.RightStickX = ProcessStickAxis(
            rawState.RightStickX, config.RightStickDeadZone, config.RightStickSensitivity,
            config.RightStickCurve, config.InvertRightX);

        processed.RightStickY = ProcessStickAxis(
            rawState.RightStickY, config.RightStickDeadZone, config.RightStickSensitivity,
            config.RightStickCurve, config.InvertRightY);

        processed.LeftTrigger = ProcessTriggerAxis(
            rawState.LeftTrigger, config.LeftTriggerDeadZone);

        processed.RightTrigger = ProcessTriggerAxis(
            rawState.RightTrigger, config.RightTriggerDeadZone);

        InputProcessed?.Invoke(this, new ProcessedInputEventArgs
        {
            ControllerId = rawState.ControllerId,
            ProcessedState = processed
        });

        return processed;
    }

    public void SetDeadZone(string controllerId, double leftStick, double rightStick, double leftTrigger, double rightTrigger)
    {
        var config = GetOrCreateConfig(controllerId);
        config.LeftStickDeadZone = leftStick;
        config.RightStickDeadZone = rightStick;
        config.LeftTriggerDeadZone = leftTrigger;
        config.RightTriggerDeadZone = rightTrigger;
    }

    public void SetStickCurve(string controllerId, string leftCurve, string rightCurve)
    {
        var config = GetOrCreateConfig(controllerId);
        config.LeftStickCurve = leftCurve;
        config.RightStickCurve = rightCurve;
    }

    public void SetInversion(string controllerId, bool invertLeftX, bool invertLeftY, bool invertRightX, bool invertRightY)
    {
        var config = GetOrCreateConfig(controllerId);
        config.InvertLeftX = invertLeftX;
        config.InvertLeftY = invertLeftY;
        config.InvertRightX = invertRightX;
        config.InvertRightY = invertRightY;
    }

    private InputConfig GetOrCreateConfig(string controllerId)
    {
        if (!_configs.TryGetValue(controllerId, out var config))
        {
            config = new InputConfig();
            _configs[controllerId] = config;
        }
        return config;
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

    private class InputConfig
    {
        public double LeftStickDeadZone { get; set; } = 0.15;
        public double RightStickDeadZone { get; set; } = 0.15;
        public double LeftTriggerDeadZone { get; set; } = 0.1;
        public double RightTriggerDeadZone { get; set; } = 0.1;
        public double LeftStickSensitivity { get; set; } = 1.0;
        public double RightStickSensitivity { get; set; } = 1.0;
        public string LeftStickCurve { get; set; } = "Linear";
        public string RightStickCurve { get; set; } = "Linear";
        public bool InvertLeftX { get; set; }
        public bool InvertLeftY { get; set; }
        public bool InvertRightX { get; set; }
        public bool InvertRightY { get; set; }
    }
}
