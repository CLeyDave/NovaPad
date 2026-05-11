using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NovaPad.Core.Events;
using NovaPad.Overlay.Notifications;
using NovaPad.Overlay.Services;
using NovaPad.Overlay.Themes;
using NovaPad.Overlay.Views;
using NovaPad.Overlay.Widgets;

namespace NovaPad.Overlay;

public partial class OverlayWindow : Window
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int GWL_EXSTYLE = -20;

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9001;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    private const int VK_O = 0x4F;

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly EventBus _eventBus = new();
    private NamedPipeServer? _pipeServer;
    private NotificationManager? _notificationManager;
    private WidgetManager? _widgetManager;
    private ThemeManager? _themeManager;
    private DebugWidgetImpl? _debugWidget;
    private HwndSource? _hwndSource;
    private bool _expandedVisible;
    private double _lastTimestamp;

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;

        var extended = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE,
            extended | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_TOPMOST);

        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource?.AddHook(WndProc);

        RegisterHotKey(hwnd, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_O);

        _themeManager = new ThemeManager();
        _notificationManager = new NotificationManager(NotificationContainer);
        _widgetManager = new WidgetManager(WidgetContainer);

        _widgetManager.Register(new ClockWidgetImpl(_themeManager));

        _debugWidget = new DebugWidgetImpl(_themeManager);
        _widgetManager.Register(_debugWidget);

        _pipeServer = new NamedPipeServer(_eventBus);
        _pipeServer.Start();

        _eventBus.Subscribe<DeviceConnectedEvent>(OnDeviceConnected);
        _eventBus.Subscribe<DeviceDisconnectedEvent>(OnDeviceDisconnected);
        _eventBus.Subscribe<BatteryChangedEvent>(OnBatteryChanged);
        _eventBus.Subscribe<ProfileChangedEvent>(OnProfileChanged);
        _eventBus.Subscribe<HudUpdateEvent>(OnHudUpdate);
        _eventBus.Subscribe<ThemeChangedEvent>(OnThemeChanged);
        _eventBus.Subscribe<WidgetToggleEvent>(OnWidgetToggle);
        _eventBus.Subscribe<OverlayConfigEvent>(OnOverlayConfig);

        CompositionTarget.Rendering += OnRendering;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            ToggleExpanded();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ToggleExpanded()
    {
        _expandedVisible = !_expandedVisible;

        if (_expandedVisible)
        {
            ExpandedWidget.Visibility = Visibility.Visible;
            ExpandedWidget.Opacity = 0;

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ExpandedWidget.BeginAnimation(OpacityProperty, fadeIn);
        }
        else
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1, To = 0,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) => ExpandedWidget.Visibility = Visibility.Collapsed;
            ExpandedWidget.BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var timestamp = (e as RenderingEventArgs)?.RenderingTime.TotalSeconds ?? 0;
        var dt = timestamp - _lastTimestamp;
        _lastTimestamp = timestamp;

        if (_debugWidget != null && dt > 0 && dt < 0.5)
        {
            _debugWidget.SetLatency(dt * 1000);
        }

        _widgetManager?.UpdateAll();
    }

    private void OnDeviceConnected(DeviceConnectedEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            var batteryMsg = evt.BatteryLevel >= 0
                ? $"{evt.Name} — Batería: {evt.BatteryLevel}%{(evt.IsCharging ? " (cargando)" : "")}"
                : evt.Name;
            _notificationManager?.ShowNotification(new NotificationData
            {
                Title = "Mando Conectado",
                Message = batteryMsg,
                Icon = NotificationIcon.Connected
            });
        });
    }

    private void OnDeviceDisconnected(DeviceDisconnectedEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            _notificationManager?.ShowNotification(new NotificationData
            {
                Title = "Mando Desconectado",
                Message = evt.Name,
                Icon = NotificationIcon.Disconnected
            });
        });
    }

    private void OnBatteryChanged(BatteryChangedEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            var icon = evt.Level <= 20
                ? NotificationIcon.BatteryLow
                : NotificationIcon.BatteryCharging;

            _notificationManager?.ShowNotification(new NotificationData
            {
                Title = evt.IsCharging ? "Cargando" : "Batería",
                Message = $"{evt.Name} — {evt.Level}%",
                Icon = icon
            });
        });
    }

    private void OnProfileChanged(ProfileChangedEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            _notificationManager?.ShowNotification(new NotificationData
            {
                Title = "Perfil Cambiado",
                Message = $"{evt.ProfileName}",
                Icon = NotificationIcon.Profile
            });
        });
    }

    private void OnHudUpdate(HudUpdateEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            HudWidget.Update(evt.Controllers);
            ExpandedWidget.Update(evt.Controllers);
        });
    }

    private void OnThemeChanged(ThemeChangedEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            Opacity = evt.BackgroundOpacity;
            _themeManager?.ApplyTheme(evt);
            _widgetManager?.ApplyThemeToAll(evt.IsDark, evt.AccentColor, evt.BackgroundOpacity);
        });
    }

    private void OnWidgetToggle(WidgetToggleEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            _widgetManager?.SetVisibility(evt.WidgetId, evt.IsVisible);
        });
    }

    private void OnOverlayConfig(OverlayConfigEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            Opacity = evt.Opacity;
            HudWidget.ApplyConfig(evt);

            var (hudH, hudV) = ParseAnchor(evt.HudAnchor);
            HudWidget.HorizontalAlignment = hudH;
            HudWidget.VerticalAlignment = hudV;
            HudWidget.Margin = new Thickness(
                hudH == HorizontalAlignment.Right ? 0 : evt.HudOffsetX,
                hudV == VerticalAlignment.Bottom ? 0 : evt.HudOffsetY,
                hudH == HorizontalAlignment.Right ? evt.HudOffsetX : 0,
                hudV == VerticalAlignment.Bottom ? evt.HudOffsetY : 0);

            _widgetManager?.SetVisibility("debug", evt.ShowFps);

            NotificationContainer.Visibility = evt.ShowNotifications ? Visibility.Visible : Visibility.Collapsed;

            var (notifH, notifV) = ParseAnchor(evt.NotificationAnchor);
            NotificationContainer.HorizontalAlignment = notifH;
            NotificationContainer.VerticalAlignment = notifV;
            NotificationContainer.Margin = new Thickness(
                notifH == HorizontalAlignment.Right ? 0 : evt.NotificationOffsetX,
                notifV == VerticalAlignment.Bottom ? 0 : evt.NotificationOffsetY,
                notifH == HorizontalAlignment.Right ? evt.NotificationOffsetX : 0,
                notifV == VerticalAlignment.Bottom ? evt.NotificationOffsetY : 0);
        });
    }

    private static (HorizontalAlignment, VerticalAlignment) ParseAnchor(string anchor)
    {
        return anchor switch
        {
            "TopLeft" => (HorizontalAlignment.Left, VerticalAlignment.Top),
            "TopCenter" => (HorizontalAlignment.Center, VerticalAlignment.Top),
            "TopRight" => (HorizontalAlignment.Right, VerticalAlignment.Top),
            "CenterLeft" => (HorizontalAlignment.Left, VerticalAlignment.Center),
            "Center" => (HorizontalAlignment.Center, VerticalAlignment.Center),
            "CenterRight" => (HorizontalAlignment.Right, VerticalAlignment.Center),
            "BottomLeft" => (HorizontalAlignment.Left, VerticalAlignment.Bottom),
            "BottomCenter" => (HorizontalAlignment.Center, VerticalAlignment.Bottom),
            "BottomRight" => (HorizontalAlignment.Right, VerticalAlignment.Bottom),
            _ => (HorizontalAlignment.Right, VerticalAlignment.Top)
        };
    }
}
