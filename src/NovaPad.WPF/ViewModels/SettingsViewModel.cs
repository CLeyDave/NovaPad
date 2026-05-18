using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaPad.Core.Events;
using NovaPad.Core.Interfaces;
using NovaPad.WPF.Services;

namespace NovaPad.WPF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsService _settings;
    private readonly IThemeService _themeService;
    private readonly IOverlayService _overlay;
    private readonly LocalizationService _localization;
    private readonly UpdateService _updater;
    private bool _loading;
    private CancellationTokenSource? _saveCts;

    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private string _theme = "Oscuro";
    [ObservableProperty] private string _language = "Español";

    [ObservableProperty] private double _notificationDuration = 3.0;
    [ObservableProperty] private int _batteryWarningThreshold = 20;
    [ObservableProperty] private bool _showConnectionNotifications = true;
    [ObservableProperty] private bool _showBatteryNotifications = true;

    public string AppVersion { get; }
    public string BuildDate { get; }
    public UpdateService Updater => _updater;

    public SettingsViewModel(IAppSettingsService settings, IThemeService themeService, IOverlayService overlay, UpdateService updater)
    {
        _settings = settings;
        _themeService = themeService;
        _overlay = overlay;
        _localization = LocalizationService.Instance;
        _updater = updater;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        AppVersion = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
        BuildDate = "2026-05";

        LoadSettings();
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        await _updater.CheckForUpdatesAsync();
    }

    [RelayCommand]
    private async Task DownloadUpdateAsync()
    {
        await _updater.DownloadUpdateAsync();
    }

    [RelayCommand]
    private void InstallUpdate()
    {
        _updater.InstallUpdate();
    }

    private void LoadSettings()
    {
        _loading = true;
        var s = _settings.Settings;
        StartWithWindows = s.StartWithWindows;
        MinimizeToTray = s.MinimizeToTray;
        Theme = _themeService.IsDarkTheme ? "Oscuro" : "Claro";
        _localization.CurrentLang = s.Language;
        Language = _localization.IsEnglish ? "English" : "Español";

        NotificationDuration = s.Notifications.DurationSeconds;
        BatteryWarningThreshold = s.Notifications.BatteryWarningThreshold;
        ShowConnectionNotifications = s.Notifications.ShowConnectionNotifications;
        ShowBatteryNotifications = s.Notifications.ShowBatteryNotifications;
        _loading = false;
    }

    partial void OnLanguageChanged(string value)
    {
        if (_loading) return;
        _localization.IsEnglish = value == "English";
        _settings.Settings.Language = _localization.CurrentLang;
        AutoSave();
    }

    partial void OnThemeChanged(string value)
    {
        if (_loading) return;
        _themeService.SetTheme(value == "Oscuro");
        AutoSave();
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_loading) return;
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (value)
                key.SetValue("NovaPad", System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
            else
                key.DeleteValue("NovaPad", false);
        }
        catch { }
        AutoSave();
    }

    partial void OnMinimizeToTrayChanged(bool value) { if (!_loading) AutoSave(); }
    partial void OnNotificationDurationChanged(double value) { if (!_loading) AutoSave(); }
    partial void OnBatteryWarningThresholdChanged(int value) { if (!_loading) AutoSave(); }
    partial void OnShowConnectionNotificationsChanged(bool value) { if (!_loading) AutoSave(); }
    partial void OnShowBatteryNotificationsChanged(bool value) { if (!_loading) AutoSave(); }

    private async void AutoSave()
    {
        var oldCts = _saveCts;
        _saveCts = new CancellationTokenSource();
        oldCts?.Cancel();
        oldCts?.Dispose();
        var token = _saveCts.Token;

        try
        {
            await Task.Delay(500, token);

            var s = _settings.Settings;
            s.StartWithWindows = StartWithWindows;
            s.MinimizeToTray = MinimizeToTray;
            s.Theme.IsDark = Theme == "Oscuro";

            s.Notifications.DurationSeconds = NotificationDuration;
            s.Notifications.BatteryWarningThreshold = BatteryWarningThreshold;
            s.Notifications.ShowConnectionNotifications = ShowConnectionNotifications;
            s.Notifications.ShowBatteryNotifications = ShowBatteryNotifications;

            _settings.Save();

            SendSettingsAsync();
        }
        catch (TaskCanceledException) { }
    }

    private void SendSettingsAsync()
    {
        try
        {
            _overlay.NotifyTheme(new CambioTema
            {
                Oscuro = Theme == "Oscuro",
                OpacidadFondo = _settings.Settings.Overlay.Opacidad
            });
        }
        catch { }
    }
}
