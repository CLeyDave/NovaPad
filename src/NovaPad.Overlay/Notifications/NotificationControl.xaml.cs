using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NovaPad.Overlay.Notifications;

public enum NotificationIcon
{
    Info,
    Connected,
    Disconnected,
    BatteryLow,
    BatteryCharging,
    Profile,
    Warning
}

public class NotificationData
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationIcon Icon { get; set; } = NotificationIcon.Info;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);
    public Action? OnDismiss { get; set; }
}

public partial class NotificationControl : System.Windows.Controls.UserControl
{
    private Storyboard? _fadeOutStoryboard;
    private Storyboard? _slideInStoryboard;
    private DateTime _showTime;
    private bool _isDismissed;

    public NotificationControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _showTime = DateTime.Now;
        _isDismissed = false;

        Opacity = 0;
        var translate = RenderTransform as TranslateTransform ?? new TranslateTransform();
        RenderTransform = translate;
        translate.X = 400;

        _slideInStoryboard = new Storyboard();

        var slideAnim = new DoubleAnimation
        {
            From = 400,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.35),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slideAnim, this);
        Storyboard.SetTargetProperty(slideAnim, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        _slideInStoryboard.Children.Add(slideAnim);

        var fadeAnim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.3)
        };
        Storyboard.SetTarget(fadeAnim, this);
        Storyboard.SetTargetProperty(fadeAnim, new PropertyPath(OpacityProperty));
        _slideInStoryboard.Children.Add(fadeAnim);

        _slideInStoryboard.Begin();
    }

    public void Bind(NotificationData data)
    {
        TitleText.Text = data.Title;
        MessageText.Text = data.Message;
        IconPath.Data = GetIconGeometry(data.Icon);

        switch (data.Icon)
        {
            case NotificationIcon.Connected:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                break;
            case NotificationIcon.Disconnected:
            case NotificationIcon.Warning:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                break;
            case NotificationIcon.BatteryLow:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                break;
            case NotificationIcon.BatteryCharging:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                break;
            case NotificationIcon.Profile:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(156, 39, 176));
                break;
            default:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0, 188, 212));
                break;
        }
    }

    public void Dismiss()
    {
        if (_isDismissed) return;
        _isDismissed = true;

        _fadeOutStoryboard = new Storyboard();
        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.25)
        };
        Storyboard.SetTarget(fadeOut, this);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
        _fadeOutStoryboard.Children.Add(fadeOut);
        _fadeOutStoryboard.Completed += (_, _) =>
        {
            Visibility = Visibility.Collapsed;
        };
        _fadeOutStoryboard.Begin();
    }

    public bool ShouldDismiss()
    {
        return !_isDismissed && (DateTime.Now - _showTime).TotalSeconds > 3;
    }

    private static Geometry GetIconGeometry(NotificationIcon icon)
    {
        var key = icon switch
        {
            NotificationIcon.Connected => "CheckmarkIcon",
            NotificationIcon.Disconnected => "CloseIcon",
            NotificationIcon.BatteryLow => "BatteryAlertIcon",
            NotificationIcon.BatteryCharging => "BatteryChargingIcon",
            NotificationIcon.Profile => "SettingsIcon",
            NotificationIcon.Warning => "AlertIcon",
            _ => "InfoIcon"
        };
        try { return (Geometry)Application.Current.FindResource(key); }
        catch { return Geometry.Empty; }
    }
}
