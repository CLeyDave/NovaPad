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
    private static readonly Mutex _mutex;
    private static readonly bool _isOnlyInstance;
    private IHost? _host;

    static App()
    {
        _mutex = new Mutex(true, MutexName, out _isOnlyInstance);
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
            CrashLogger.LogException(e.Exception, "App.DispatcherUnhandledException");
            e.Handled = true;
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "NovaPad", "logs", "novapad-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            Log.Information("NovaPad starting...");

            _host = Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<NavigationService>();
                    services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

                    services.AddSingleton<IAppSettingsService, AppSettingsService>();

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

                    services.AddSingleton<OverlayClient>();
                    services.AddSingleton<OverlayProcessManager>();
                    services.AddSingleton<ILightingService, LightingService>();

                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ControllerListViewModel>();
                    services.AddTransient<ControllerDetailViewModel>();
                    services.AddTransient<DeviceInfoViewModel>();
                    services.AddTransient<BatteryViewModel>();
                    services.AddTransient<RGBViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<OverlaySettingsViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            await _host.StartAsync();

            var settings = _host.Services.GetRequiredService<IAppSettingsService>();
            settings.Load();

            var themeService = _host.Services.GetRequiredService<IThemeService>();

            var controllerService = _host.Services.GetRequiredService<IControllerManagerService>();
            await controllerService.StartDetectionAsync();
            await controllerService.ScanForControllersAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();

            if (settings.Settings.AutoStartOverlay)
            {
                var overlayManager = _host.Services.GetRequiredService<OverlayProcessManager>();
                await overlayManager.StartAsync();
            }

            mainWindow.Show();

            controllerService.NotifyConnectedControllers();

            Log.Information("NovaPad started successfully");
        }
        catch (Exception ex)
        {
            CrashLogger.LogException(ex, "OnStartup");
            MessageBox.Show(
                $"NovaPad no pudo iniciarse.\n\nError: {ex.Message}\n\n" +
                $"Se guardó un reporte en:\n{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NovaPad", "crashes")}",
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
            _mutex.ReleaseMutex();
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
