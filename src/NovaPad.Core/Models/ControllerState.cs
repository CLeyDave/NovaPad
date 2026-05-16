using NovaPad.Core.Enums;

namespace NovaPad.Core.Models;

public class ControllerState
{
    public string ControllerId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public bool A { get; set; }
    public bool B { get; set; }
    public bool X { get; set; }
    public bool Y { get; set; }
    public bool DPadUp { get; set; }
    public bool DPadDown { get; set; }
    public bool DPadLeft { get; set; }
    public bool DPadRight { get; set; }
    public bool LeftBumper { get; set; }
    public bool RightBumper { get; set; }
    public bool Start { get; set; }
    public bool Back { get; set; }
    public bool Guide { get; set; }
    public bool Share { get; set; }
    public bool Options { get; set; }
    public bool LeftStickClick { get; set; }
    public bool RightStickClick { get; set; }
    public bool TouchpadClick { get; set; }
    public bool TouchpadTouch { get; set; }

    public double LeftStickX { get; set; }
    public double LeftStickY { get; set; }
    public double RightStickX { get; set; }
    public double RightStickY { get; set; }

    public double LeftTrigger { get; set; }
    public double RightTrigger { get; set; }

    public double GyroPitch { get; set; }
    public double GyroYaw { get; set; }
    public double GyroRoll { get; set; }
    public double AccelX { get; set; }
    public double AccelY { get; set; }
    public double AccelZ { get; set; }

    public int TouchpadX { get; set; }
    public int TouchpadY { get; set; }

    public int BatteryLevel { get; set; } = -1;
    public bool IsCharging { get; set; }
    public double LatencyMs { get; set; }

    public bool this[ButtonType button] => button switch
    {
        ButtonType.A => A,
        ButtonType.B => B,
        ButtonType.X => X,
        ButtonType.Y => Y,
        ButtonType.DPadUp => DPadUp,
        ButtonType.DPadDown => DPadDown,
        ButtonType.DPadLeft => DPadLeft,
        ButtonType.DPadRight => DPadRight,
        ButtonType.LeftBumper => LeftBumper,
        ButtonType.RightBumper => RightBumper,
        ButtonType.Start => Start,
        ButtonType.Back => Back,
        ButtonType.Guide => Guide,
        ButtonType.Share => Share,
        ButtonType.Options => Options,
        ButtonType.LeftStick => LeftStickClick,
        ButtonType.RightStick => RightStickClick,
        ButtonType.TouchpadClick => TouchpadClick,
        ButtonType.Gyro => GyroPitch != 0 || GyroYaw != 0 || GyroRoll != 0,
        ButtonType.Accelerometer => AccelX != 0 || AccelY != 0 || AccelZ != 0,
        ButtonType.Custom => false,
        _ => false
    };
}
