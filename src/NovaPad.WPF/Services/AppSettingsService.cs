using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NovaPad.Core.Interfaces;
using NovaPad.Core.Models;
using Serilog;

namespace NovaPad.WPF.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly string _filePath;
    private readonly string _dir;
    private AppSettings _settings = new();

    public AppSettings Settings => _settings;

    public AppSettingsService()
    {
        _dir = AppContext.BaseDirectory;
        _filePath = Path.Combine(_dir, "settings.json");
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                Log.Information("[AppSettings] Loaded from {Path}", _filePath);
            }
            else
            {
                Log.Information("[AppSettings] No settings file at {Path}, using defaults", _filePath);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[AppSettings] Failed to load, using defaults");
            _settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_dir);
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals });
            File.WriteAllText(_filePath, json);
            Log.Information("[AppSettings] Saved to {Path}", _filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[AppSettings] Failed to save");
        }
    }
}
