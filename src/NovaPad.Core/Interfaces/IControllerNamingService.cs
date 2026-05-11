namespace NovaPad.Core.Interfaces;

public interface IControllerNamingService
{
    string? GetCustomName(string controllerId);
    void SetCustomName(string controllerId, string name);
    void Load();
    void Save();
}
