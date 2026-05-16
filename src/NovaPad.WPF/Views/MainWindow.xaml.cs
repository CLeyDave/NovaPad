using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NovaPad.Core.Interfaces;
using NovaPad.WPF.Infrastructure;
using NovaPad.WPF.Services;
using NovaPad.WPF.ViewModels;
using Serilog;

namespace NovaPad.WPF.Views;

public partial class MainWindow : Window
{
    private readonly NavigationService _navigation;
    private readonly MainViewModel _viewModel;
    private readonly IAppSettingsService _settings;
    private bool _isMaximized;
    private Rect _normalBounds;

    public MainWindow(MainViewModel viewModel, NavigationService navigation, IAppSettingsService settings)
    {
        var sw = Stopwatch.StartNew();
        Log.Information("[MainWindow] Constructor start.");

        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _navigation = navigation;
        _settings = settings;

        navigation.SetContentControl(MainContent);
        RegisterViews();
        navigation.NavigateTo("DashboardView");

        LoadSettings();
        Loaded += OnLoaded;
        Closing += OnClosing;

        sw.Stop();
        Log.Information("[MainWindow] Constructor done in {Ms}ms.", sw.ElapsedMilliseconds);
    }

    private void LoadSettings()
    {
        var ws = _settings.Settings.Window;
        if (!ws.IsMaximized && !double.IsNaN(ws.Left) && ws.Width > 0)
        {
            Left = ws.Left;
            Top = ws.Top;
            Width = ws.Width;
            Height = ws.Height;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadAppIcon();
        _normalBounds = new Rect(Left, Top, Width, Height);
        if (_settings.Settings.Window.IsMaximized)
            MaximizeToWorkArea();
    }

    private void LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/NovaPadIcon.png");
            var bmp = new BitmapImage(uri);
            bmp.Freeze();
            Icon = bmp;
            TrayIcon.IconSource = bmp;
            Log.Information("[MainWindow] App icon loaded from PNG resource.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[MainWindow] Failed to load app icon");
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowSettings();
        _settings.Save();

        if (_settings.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            TrayIcon.Visibility = Visibility.Visible;
            Hide();
            return;
        }

        TrayIcon.Dispose();
        Application.Current.Shutdown();
    }

    private void SaveWindowSettings()
    {
        var ws = _settings.Settings.Window;
        if (_isMaximized)
        {
            ws.IsMaximized = true;
            ws.Left = _normalBounds.Left;
            ws.Top = _normalBounds.Top;
            ws.Width = _normalBounds.Width;
            ws.Height = _normalBounds.Height;
        }
        else
        {
            ws.IsMaximized = false;
            ws.Left = Left;
            ws.Top = Top;
            ws.Width = Width;
            ws.Height = Height;
        }
    }

    private void ShowWindowFromTray(object sender, RoutedEventArgs e)
        => ShowWindow();

    private void TrayShowClick(object sender, RoutedEventArgs e)
        => ShowWindow();

    private void TrayExitClick(object sender, RoutedEventArgs e)
    {
        TrayIcon.Visibility = Visibility.Collapsed;
        Application.Current.Shutdown();
    }

    private void ShowWindow()
    {
        TrayIcon.Visibility = Visibility.Collapsed;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void MaximizeToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        _isMaximized = true;
        WindowBorder.CornerRadius = new CornerRadius(0);
        WindowBorder.BorderThickness = new Thickness(0);
        Left = workArea.Left;
        Top = workArea.Top;
        Width = workArea.Width;
        Height = workArea.Height;
        UpdateMaximizeIcon();
    }

    private void RestoreFromMaximized()
    {
        _isMaximized = false;
        WindowBorder.CornerRadius = new CornerRadius(8);
        WindowBorder.BorderThickness = new Thickness(1);
        Left = _normalBounds.Left;
        Top = _normalBounds.Top;
        Width = _normalBounds.Width;
        Height = _normalBounds.Height;
        UpdateMaximizeIcon();
    }

    private void UpdateMaximizeIcon()
    {
        MaximizeIcon.Data = _isMaximized
            ? (System.Windows.Media.Geometry)System.Windows.Application.Current.FindResource("RestoreIcon")
            : (System.Windows.Media.Geometry)System.Windows.Application.Current.FindResource("MaximizeIcon");
    }

    private void RegisterViews()
    {
        _navigation.RegisterView("DashboardView", () => new DashboardView(
            App.GetService<DashboardViewModel>()));
        _navigation.RegisterView("ControllerListView", () => new ControllerListView(
            App.GetService<ControllerListViewModel>()));
        _navigation.RegisterView("ControllerDetailView", () => new ControllerDetailView(
            App.GetService<ControllerDetailViewModel>()));
        _navigation.RegisterView("DeviceInfoView", () => new DeviceInfoView(
            App.GetService<DeviceInfoViewModel>()));

        _navigation.RegisterView("BatteryView", () => new BatteryView(
            App.GetService<BatteryViewModel>()));
        _navigation.RegisterView("RGBView", () => new RGBView(
            App.GetService<RGBViewModel>()));
        _navigation.RegisterView("SettingsView", () => new SettingsView(
            App.GetService<SettingsViewModel>()));
        _navigation.RegisterView("OverlaySettingsView", () => new PaginaOverlay(
            App.GetService<AdminOverlayVm>()));
    }

    private void MinimizeClick(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeClick(object sender, RoutedEventArgs e)
    {
        if (_isMaximized)
            RestoreFromMaximized();
        else
            MaximizeToWorkArea();
    }

    private void CloseClick(object sender, RoutedEventArgs e)
        => Close();

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
        {
            MaximizeClick(sender, e);
            return;
        }
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // Ctrl+Shift+O hotkey (backup for overlay panel)
    private const int WM_HOTKEY = 0x0312;
    private const int IdOverlayHotkey = 9002;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    private const int VK_O = 0x4F;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);
        RegisterHotKey(hwnd, IdOverlayHotkey, MOD_CONTROL | MOD_SHIFT, VK_O);
        Log.Information("[MainWindow] Hotkey Ctrl+Shift+O registered");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == IdOverlayHotkey)
        {
            try
            {
                var overlay = App.GetService<IOverlayService>();
                overlay.TogglePanel();
            }
            catch { }
            handled = true;
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(hwnd, IdOverlayHotkey);
        base.OnClosed(e);
    }
}
