namespace NovaPad.Core.Interfaces;

public enum LightEffect
{
    Static,
    Pulse,
    Rainbow,
    BatteryLevel,
    Strobe,
    Heartbeat,
    Fire,
    Disco,
    Wave,
    Meteor,
    Aurora,
    Storm
}

public interface ILightingService
{
    bool IsSupported(string controllerId);
    void SetColor(string controllerId, byte r, byte g, byte b);
    void SetEffect(string controllerId, LightEffect effect, byte r, byte g, byte b, byte r2 = 0, byte g2 = 0, byte b2 = 0);
    void Stop(string controllerId);
    void StopAll();
}