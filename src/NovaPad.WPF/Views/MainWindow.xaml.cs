using System.Windows;
using System.Windows.Input;
using NovaPad.Core.Interfaces;
using NovaPad.WPF.Infrastructure;
using NovaPad.WPF.Services;
using NovaPad.WPF.ViewModels;

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
    }

    private void LoadSettings()
    {
        var ws = _settings.Settings.Window;
        if (!ws.IsMaximized)
        {
            Left = ws.Left;
            Top = ws.Top;
            Width = ws.Width;
            Height = ws.Height;
        }
        _normalBounds = new Rect(Left, Top, Width, Height);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_settings.Settings.Window.IsMaximized)
            MaximizeToWorkArea();
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
        }
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
        _navigation.RegisterView("OverlaySettingsView", () => new OverlaySettingsView(
            App.GetService<OverlaySettingsViewModel>()));
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
        if (e.ClickCount == 2)
        {
            MaximizeClick(sender, e);
            return;
        }
        DragMove();
    }
}
