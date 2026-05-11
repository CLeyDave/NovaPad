using NovaPad.Core.Models;

namespace NovaPad.Core.Interfaces;

public interface IControllerManagerService
{
    event EventHandler<ControllerInfo>? ControllerConnected;
    event EventHandler<ControllerInfo>? ControllerDisconnected;
    event EventHandler<ControllerState>? InputReceived;
    event EventHandler<ControllerInfo>? ControllerUpdated;

    IReadOnlyList<ControllerInfo> ConnectedControllers { get; }
    ControllerState? GetCurrentState(string controllerId);
    Task StartDetectionAsync();
    Task StopDetectionAsync();
    Task<bool> SetRumbleAsync(string controllerId, double leftMotor, double rightMotor);
    Task<bool> SetLedColorAsync(string controllerId, byte r, byte g, byte b);
    Task<bool> RenameControllerAsync(string controllerId, string newName);
    Task<bool> DisconnectControllerAsync(string controllerId);
    Task<IReadOnlyList<ControllerInfo>> ScanForControllersAsync();
    void NotifyConnectedControllers();
}
