using NovaPad.Core.Models;

namespace NovaPad.Core.Interfaces;

public interface IMacroEngine
{
    event EventHandler<MacroAction>? MacroStarted;
    event EventHandler<MacroAction>? MacroCompleted;

    bool IsRunning { get; }
    Task<bool> ExecuteMacroAsync(List<MacroAction> actions, string controllerId);
    Task<bool> EvaluateTriggersAsync(ControllerState state, string controllerId);
    void Stop();
    Task<List<MacroAction>> RecordSequenceAsync(TimeSpan maxDuration);
}
