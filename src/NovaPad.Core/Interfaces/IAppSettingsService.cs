using NovaPad.Core.Models;

namespace NovaPad.Core.Interfaces;

public interface IAppSettingsService
{
    AppSettings Settings { get; }
    void Load();
    void Save();
}
