namespace NovaPad.Core.Models;

public class InputProcessingConfig
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
