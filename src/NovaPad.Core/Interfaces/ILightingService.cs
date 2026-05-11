namespace NovaPad.Core.Interfaces;

public enum LightEffect
{
    Static,
    Pulse,
    Rainbow,
    BatteryLevel
}

public interface ILightingService
{
    bool IsSupported(string controllerId);
    void SetColor(string controllerId, byte r, byte g, byte b);
    void SetEffect(string controllerId, LightEffect effect, byte r, byte g, byte b);
    void Stop(string controllerId);
    void StopAll();
}