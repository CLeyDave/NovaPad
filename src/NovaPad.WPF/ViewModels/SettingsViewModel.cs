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
    private readonly OverlayClient _overlayClient;

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

    public SettingsViewModel(IAppSettingsService settings, IThemeService themeService, OverlayClient overlayClient)
    {
        _settings = settings;
        _themeService = themeService;
        _overlayClient = overlayClient;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        AppVersion = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
        BuildDate = "2025-03";

        LoadSettings();
    }

    private void LoadSettings()
    {
        var s = _settings.Settings;
        StartWithWindows = s.StartWithWindows;
        MinimizeToTray = s.MinimizeToTray;
        Theme = _themeService.IsDarkTheme ? "Oscuro" : "Claro";

        NotificationDuration = s.Notifications.DurationSeconds;
        BatteryWarningThreshold = s.Notifications.BatteryWarningThreshold;
        ShowConnectionNotifications = s.Notifications.ShowConnectionNotifications;
        ShowBatteryNotifications = s.Notifications.ShowBatteryNotifications;
    }

    partial void OnThemeChanged(string value)
    {
        _themeService.SetTheme(value == "Oscuro");
        AutoSave();
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
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

    partial void OnMinimizeToTrayChanged(bool value) => AutoSave();
    partial void OnNotificationDurationChanged(double value) => AutoSave();
    partial void OnBatteryWarningThresholdChanged(int value) => AutoSave();
    partial void OnShowConnectionNotificationsChanged(bool value) => AutoSave();
    partial void OnShowBatteryNotificationsChanged(bool value) => AutoSave();

    private void AutoSave()
    {
        var s = _settings.Settings;
        s.StartWithWindows = StartWithWindows;
        s.MinimizeToTray = MinimizeToTray;
        s.Theme.IsDark = Theme == "Oscuro";

        s.Notifications.DurationSeconds = NotificationDuration;
        s.Notifications.BatteryWarningThreshold = BatteryWarningThreshold;
        s.Notifications.ShowConnectionNotifications = ShowConnectionNotifications;
        s.Notifications.ShowBatteryNotifications = ShowBatteryNotifications;

        _settings.Save();

        _ = SendSettingsAsync();
    }

    private async Task SendSettingsAsync()
    {
        try
        {
            await _overlayClient.SendAsync(new ThemeChangedEvent
            {
                IsDark = Theme == "Oscuro",
                BackgroundOpacity = _settings.Settings.Overlay.Opacity
            });
        }
        catch { }
    }
}
