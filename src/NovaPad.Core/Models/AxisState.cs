namespace NovaPad.Core.Models;

public class AxisState
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Magnitude => Math.Sqrt(X * X + Y * Y);
    public double Angle => Math.Atan2(Y, X) * (180.0 / Math.PI);
    public double DeadZone { get; set; }
    public bool IsInDeadZone => Magnitude <= DeadZone;
    public double NormalizedX => IsInDeadZone ? 0 : X;
    public double NormalizedY => IsInDeadZone ? 0 : Y;
}
