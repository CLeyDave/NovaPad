using NovaPad.Core.Enums;

namespace NovaPad.Core.Models;

public class InputState
{
    public ButtonType Type { get; set; }
    public bool IsPressed { get; set; }
    public double Value { get; set; }
    public double RawValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public bool IsConsumed { get; set; }
}
