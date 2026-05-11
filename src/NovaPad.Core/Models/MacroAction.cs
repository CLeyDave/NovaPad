using NovaPad.Core.Enums;

namespace NovaPad.Core.Models;

public class MacroAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MacroActionType ActionType { get; set; }
    public ButtonType Button { get; set; }
    public double Value { get; set; }
    public int DurationMs { get; set; }
    public int DelayMs { get; set; }
    public int RepeatCount { get; set; } = 1;
    public int RepeatDelayMs { get; set; }
}

public enum MacroActionType
{
    ButtonPress,
    ButtonRelease,
    ButtonTap,
    Delay,
    SetAxis,
    WaitForInput,
    Repeat,
    StopMacro
}
