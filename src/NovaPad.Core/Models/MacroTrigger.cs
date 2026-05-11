using NovaPad.Core.Enums;

namespace NovaPad.Core.Models;

public class MacroTrigger
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MacroTriggerType Type { get; set; }
    public ButtonType Button { get; set; }
    public string? GameExecutable { get; set; }
    public string? ProfileId { get; set; }
}

public enum MacroTriggerType
{
    OnButtonPress,
    OnButtonHold,
    OnButtonDoubleTap,
    OnGameStart,
    OnGameExit,
    OnProfileActivate,
    OnDeviceConnect
}
