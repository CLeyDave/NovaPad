using System.IO;
using System.Text.Json;
using NovaPad.Core.Interfaces;

namespace NovaPad.WPF.Services;

public class ControllerNamingService : IControllerNamingService
{
    private readonly string _filePath;
    private Dictionary<string, string> _names = new();

    public ControllerNamingService()
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "names.json");
    }

    public string? GetCustomName(string controllerId)
    {
        return _names.TryGetValue(controllerId, out var name) ? name : null;
    }

    public void SetCustomName(string controllerId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            _names.Remove(controllerId);
        else
            _names[controllerId] = name;
        Save();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _names = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch
        {
            _names = new();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_names, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }
}
