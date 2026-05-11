using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NovaPad.Core.Events;

namespace NovaPad.Overlay.Widgets;

public partial class HudControl : UserControl
{
    private bool _showBattery = true;
    private bool _showLatency = true;
    private bool _showPollingRate;
    private bool _showProfileName = true;
    private bool _showConnectionStatus = true;
    private bool _showControllerType = true;

    public HudControl()
    {
        InitializeComponent();
    }

    public void ApplyConfig(OverlayConfigEvent config)
    {
        _showBattery = config.ShowBattery;
        _showLatency = config.ShowLatency;
        _showPollingRate = config.ShowPollingRate;
        _showProfileName = config.ShowProfileName;
        _showConnectionStatus = config.ShowConnectionStatus;
        _showControllerType = config.ShowControllerType;
    }

    public void Update(List<HudControllerData> controllers)
    {
        ItemsStack.Children.Clear();

        if (controllers.Count == 0)
        {
            ItemsStack.Children.Add(new TextBlock
            {
                Text = "Sin mandos",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            });
            return;
        }

        foreach (var ctrl in controllers)
        {
            ItemsStack.Children.Add(BuildCard(ctrl));
        }
    }

    private Border BuildCard(HudControllerData ctrl)
    {
        var root = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

        var headerRow = new Grid();
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var dot = new Ellipse
        {
            Width = 8, Height = 8,
            Fill = new SolidColorBrush(ctrl.IsConnected ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(dot, 0);
        headerRow.Children.Add(dot);

        var name = new TextBlock
        {
            Text = ctrl.Name,
            FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(name, 1);
        headerRow.Children.Add(name);

        if (_showBattery)
        {
            var battery = new TextBlock
            {
                Text = ctrl.BatteryLevel >= 0 ? $"{ctrl.BatteryLevel}%" : "--",
                FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(BatteryColor(ctrl.BatteryLevel)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(battery, 2);
            headerRow.Children.Add(battery);
        }

        root.Children.Add(headerRow);

        var details = new TextBlock
        {
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
            Margin = new Thickness(16, 2, 0, 0)
        };

        var parts = new List<string>();
        if (_showLatency && ctrl.LatencyMs > 0) parts.Add($"{ctrl.LatencyMs:F1}ms");
        if (_showPollingRate && ctrl.PollingRateHz > 0) parts.Add($"{ctrl.PollingRateHz}Hz");
        if (_showProfileName && !string.IsNullOrEmpty(ctrl.ProfileName)) parts.Add(ctrl.ProfileName);

        details.Text = parts.Count > 0 ? string.Join(" · ", parts) : "";

        if (!string.IsNullOrEmpty(details.Text))
            root.Children.Add(details);

        return new Border
        {
            Child = root,
            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8)
        };
    }

    private static Color BatteryColor(int level)
    {
        if (level < 0) return Color.FromRgb(170, 170, 170);
        if (level <= 20) return Color.FromRgb(244, 67, 54);
        if (level <= 70) return Color.FromRgb(255, 193, 7);
        return Color.FromRgb(76, 175, 80);
    }
}
