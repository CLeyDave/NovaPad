using NovaPad.Core.Models;

namespace NovaPad.Core.Interfaces;

public interface IInputProcessingService
{
    event EventHandler<ProcessedInputEventArgs>? InputProcessed;

    ControllerState ProcessRawInput(ControllerState rawState, string profileId);
    void SetDeadZone(string controllerId, double leftStick, double rightStick, double leftTrigger, double rightTrigger);
    void SetStickCurve(string controllerId, string leftCurve, string rightCurve);
    void SetInversion(string controllerId, bool invertLeftX, bool invertLeftY, bool invertRightX, bool invertRightY);
}

public class ProcessedInputEventArgs : EventArgs
{
    public string ControllerId { get; set; } = string.Empty;
    public ControllerState ProcessedState { get; set; } = new();
}
