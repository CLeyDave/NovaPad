using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;
using Serilog;

namespace NovaPad.WPF.Services;

public class LightingService : ILightingService, IDisposable
{
    private readonly LightingEngine _engine;

    public LightingService(IControllerManagerService controllerManager)
    {
        _engine = new LightingEngine(controllerManager);
    }

    public bool IsSupported(string controllerId) => _engine.IsSupported(controllerId);

    public void SetColor(string controllerId, byte r, byte g, byte b)
    {
        Log.Information("[LightingService] SetColor: controller={Id}, RGB({R},{G},{B})", controllerId, r, g, b);
        _engine.Enqueue(new LightingCommand
        {
            ControllerId = controllerId,
            Type = LightingCommandType.SetEffect,
            Effect = LightEffect.Static,
            R = r, G = g, B = b,
            Priority = EffectPriority.User
        });
    }

    public void SetEffect(string controllerId, LightEffect effect, byte r, byte g, byte b, byte r2 = 0, byte g2 = 0, byte b2 = 0)
    {
        Log.Information("[LightingService] SetEffect: controller={Id}, effect={Effect}, RGB({R},{G},{B}), RGB2({R2},{G2},{B2})", controllerId, effect, r, g, b, r2, g2, b2);
        _engine.Enqueue(new LightingCommand
        {
            ControllerId = controllerId,
            Type = LightingCommandType.SetEffect,
            Effect = effect,
            R = r, G = g, B = b,
            R2 = r2, G2 = g2, B2 = b2,
            Priority = EffectPriority.User
        });
    }

    public void Stop(string controllerId)
    {
        Log.Information("[LightingService] Stop: controller={Id}", controllerId);
        _engine.Enqueue(new LightingCommand
        {
            ControllerId = controllerId,
            Type = LightingCommandType.Stop,
            Priority = EffectPriority.User
        });
    }

    public void StopAll()
    {
        Log.Information("[LightingService] StopAll");
        _engine.StopAll();
    }

    public void Dispose() => _engine.Dispose();
}
