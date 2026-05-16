using NovaPad.Core.Interfaces;

namespace NovaPad.Core.Models;

public enum LightingCommandType
{
    SetEffect,
    Stop
}

public enum EffectPriority
{
    User = 0,
    Battery = 1,
    Notification = 2,
    Critical = 3
}

public class LightingCommand
{
    public string ControllerId { get; init; } = string.Empty;
    public LightingCommandType Type { get; init; }
    public LightEffect? Effect { get; init; }
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
    public byte R2 { get; init; }
    public byte G2 { get; init; }
    public byte B2 { get; init; }
    public EffectPriority Priority { get; init; } = EffectPriority.User;
}
