using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using NovaPad.Core.Interfaces;
using NovaPad.HID.Services;
using NovaPad.WPF.Infrastructure;
using NovaPad.WPF.Services;
using NovaPad.WPF.ViewModels;
using NovaPad.WPF.Views;

namespace NovaPad.WPF;

public partial class App
{
    private const string MutexName = "NovaPad_WPF_SingleInstance";
    private static readonly Mutex _mutex = new(false, MutexName);
    private static readonly bool _isOnlyInstance;
    private IHost? _host;
    private static string? _logPath;

    static App()
    {
        try
        {
            _isOnlyInstance = _mutex.WaitOne(TimeSpan.Zero, false);
        }
        catch (AbandonedMutexException)
        {
            _isOnlyInstance = true;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    private const int SW_RESTORE = 9;

    public App()
    {
        if (!_isOnlyInstance)
        {
            var existing = FindWindow(null, "NovaPad");
            if (existing != IntPtr.Zero)
            {
                ShowWindowAsync(existing, SW_RESTORE);
                SetForegroundWindow(existing);
            }
            Environment.Exit(0);
            return;
        }

        CrashLogger.Initialize();

        DispatcherUnhandledException += (s, e) =>
        {
            Log.Error(e.Exception, "[Dispatcher] Unhandled exception");
            CrashLogger.LogException(e.Exception, "App.DispatcherUnhandledException");
            e.Handled = true;
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovaPad", "logs");
            Directory.CreateDirectory(logDir);

            var logTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _logPath = Path.Combine(logDir, $"startup-{logTimestamp}.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(_logPath, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("=== NovaPad Launch ===");
            Log.Information("Version: 3.1.3");
            Log.Information("OS: {Os} | 64-bit: {Bits} | Runtime: {Runtime}",
                Environment.OSVersion, Environment.Is64BitProcess, Environment.Version);
            Log.Information("CommandLine: {Cmd}", Environment.CommandLine);
            Log.Information("WorkDir: {Dir}", Environment.CurrentDirectory);

            Log.Information("[{Elapsed}] Building host...", sw.Elapsed);
            _host = Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<NavigationService>();
                    services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

                    services.AddSingleton<IAppSettingsService, AppSettingsService>();
                    services.AddSingleton<Services.UpdateService>();

                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<INotificationService, NotificationService>();

                    services.AddSingleton<SdlBatteryService>();
                    services.AddSingleton<HidDirectBatteryService>();
                    services.AddSingleton<IBatteryService>(sp =>
                        new AggregateBatteryService(new IBatteryService[]
                        {
                            sp.GetRequiredService<SdlBatteryService>(),
                            sp.GetRequiredService<HidDirectBatteryService>(),
                        }));

                    services.AddSingleton<IControllerManagerService, RealControllerManagerService>();
                    services.AddSingleton<IControllerNamingService, ControllerNamingService>();
                    services.AddSingleton<IInputProcessingService, InputProcessingService>();

                    services.AddSingleton<LocalizationService>();
                    services.AddSingleton<ILightingService, LightingService>();
                    services.AddSingleton<Core.Interfaces.IOverlayService, Overlay.VentanaFondo>();
                    services.AddSingleton<Overlay.VentanaFondo>();

                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ControllerListViewModel>();
                    services.AddTransient<ControllerDetailViewModel>();
                    services.AddTransient<DeviceInfoViewModel>();
                    services.AddTransient<BatteryViewModel>();
                    services.AddTransient<RGBViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddSingleton<AdminOverlayVm>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            Log.Information("[{Elapsed}] Host built, resolving services...", sw.Elapsed);
            _ = _host.StartAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Log.Error(t.Exception, "[Startup] Host start failed");
            });

            var settings = _host.Services.GetRequiredService<IAppSettingsService>();
            var controllerService = _host.Services.GetRequiredService<IControllerManagerService>();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            var lighting = _host.Services.GetRequiredService<ILightingService>();
            Log.Information("[{Elapsed}] Services resolved.", sw.Elapsed);

            Log.Information("[{Elapsed}] Loading settings...", sw.Elapsed);
            settings.Load();
            Log.Information("  AutoStartOverlay={Overlay}", settings.Settings.AutoStartOverlay);

            Log.Information("[{Elapsed}] Starting controller detection...", sw.Elapsed);
            await controllerService.StartDetectionAsync();

            Log.Information("[{Elapsed}] Showing window...", sw.Elapsed);
            mainWindow.Show();

            // Defer: scan, RGB restore, overlay, update check
            _ = Task.Run(async () =>
            {
                try
                {
                    Log.Information("[Background] Scanning controllers...");
                    await controllerService.ScanForControllersAsync();
                    Log.Information("[Background] Controller scan completed.");

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        controllerService.NotifyConnectedControllers();

                        // Restore saved RGB states
                        foreach (var ctrl in controllerService.ConnectedControllers)
                        {
                            var saved = settings.Settings.RgbState.GetValueOrDefault(ctrl.Id);
                            if (saved != null)
                            {
                                lighting.SetEffect(ctrl.Id, (LightEffect)saved.EffectIndex,
                                    saved.R, saved.G, saved.B, saved.R2, saved.G2, saved.B2);
                            }
                        }

                        if (settings.Settings.AutoStartOverlay)
                        {
                            Log.Information("[Background] Starting overlay...");
                            try
                            {
                                var overlayVm = _host.Services.GetRequiredService<AdminOverlayVm>();
                                overlayVm.StartLayerCommand.Execute(null);
                                Log.Information("[Background] Overlay auto-start completed, IsActive={Active}",
                                    _host.Services.GetRequiredService<Core.Interfaces.IOverlayService>().IsActive);
                            }
                            catch (Exception exOv)
                            {
                                Log.Error(exOv, "[Background] Overlay auto-start failed");
                            }
                        }

                    });

                        // Check for updates at startup (background, no dispatcher needed for HTTP)
                        try
                        {
                            Log.Information("[Background] Checking for updates...");
                            var updater = _host.Services.GetRequiredService<Services.UpdateService>();
                            await updater.CheckForUpdatesAsync();

                            if (updater.Status == Services.UpdateStatus.Available)
                            {
                                var tag = updater.LatestRelease?.TagName ?? "desconocida";
                                Log.Information("[Background] Update available: {Tag}", tag);

                                await Application.Current.Dispatcher.InvokeAsync(async () =>
                                {
                                    var dialog = new Views.UpdatePromptWindow(tag);
                                    dialog.Owner = mainWindow;
                                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                                    var result = dialog.ShowDialog();

                                    if (result == true)
                                    {
                                        switch (dialog.Choice)
                                        {
                                            case Views.UpdateChoice.DownloadInstall:
                                                Log.Information("[Background] User chose download & install");
                                                await updater.DownloadUpdateAsync();
                                                if (updater.Status == Services.UpdateStatus.ReadyToInstall)
                                                    updater.InstallUpdate();
                                                break;
                                            case Views.UpdateChoice.DownloadOnly:
                                                Log.Information("[Background] User chose download only");
                                                await updater.DownloadUpdateAsync();
                                                break;
                                        }
                                    }
                                });
                            }
                            else if (updater.Status == Services.UpdateStatus.UpToDate)
                            {
                                Log.Information("[Background] Already up to date ({Ver})", updater.CurrentVersion);
                            }
                            else if (updater.Status == Services.UpdateStatus.Error)
                            {
                                Log.Warning("[Background] Update check failed");
                            }
                        }
                        catch (Exception exUpd)
                        {
                            Log.Error(exUpd, "[Background] Update check error");
                        }
                    }
                    catch (Exception ex)
                {
                    Log.Error(ex, "[Background] Error in deferred startup tasks");
                }
            });

            sw.Stop();
            Log.Information("[{Elapsed}] NovaPad started successfully (total {TotalMs}ms)",
                sw.Elapsed, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log.Error(ex, "[{Elapsed}] FATAL startup error after {TotalMs}ms",
                sw.Elapsed, sw.ElapsedMilliseconds);
            CrashLogger.LogException(ex, "OnStartup");
            MessageBox.Show(
                $"NovaPad no pudo iniciarse.\n\nError: {ex.Message}\n\n" +
                $"Se guardó un reporte en:\n{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NovaPad", "crashes")}\n\n" +
                $"Log: {_logPath}",
                "Error al iniciar NovaPad",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("NovaPad shutting down.");
            if (_isOnlyInstance)
            {
                try { _mutex.ReleaseMutex(); } catch { }
            }
            _mutex.Dispose();
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            Log.CloseAndFlush();
        }
        catch { }
        base.OnExit(e);
    }

    public static T GetService<T>() where T : class
    {
        var app = (App)Current;
        return app._host!.Services.GetRequiredService<T>();
    }
}
