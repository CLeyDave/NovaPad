using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovaPad.Core.Events;

namespace NovaPad.Overlay.Views;

public class ExpandedItem
{
    public string Name { get; set; } = string.Empty;
    public int BatteryLevel { get; set; } = -1;
    public bool IsConnected { get; set; } = true;
    public string Details { get; set; } = string.Empty;

    public SolidColorBrush StatusColor =>
        new(IsConnected ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));

    public string BatteryText => BatteryLevel >= 0 ? $"{BatteryLevel}%" : "--";

    public SolidColorBrush BatteryColor
    {
        get
        {
            if (BatteryLevel < 0) return new SolidColorBrush(Color.FromRgb(170, 170, 170));
            if (BatteryLevel <= 20) return new SolidColorBrush(Color.FromRgb(244, 67, 54));
            if (BatteryLevel <= 70) return new SolidColorBrush(Color.FromRgb(255, 193, 7));
            return new SolidColorBrush(Color.FromRgb(76, 175, 80));
        }
    }
}

public partial class ExpandedPanel : UserControl
{
    private readonly ObservableCollection<ExpandedItem> _controllers = new();

    public ExpandedPanel()
    {
        InitializeComponent();
        ControllerList.ItemsSource = _controllers;
        UpdateSystemInfo();
    }

    public void Update(List<HudControllerData> controllers)
    {
        _controllers.Clear();
        foreach (var c in controllers)
        {
            var parts = new List<string>();
            if (c.LatencyMs > 0) parts.Add($"{c.LatencyMs:F1}ms");
            if (c.PollingRateHz > 0) parts.Add($"{c.PollingRateHz}Hz");
            if (!string.IsNullOrEmpty(c.ProfileName)) parts.Add(c.ProfileName);
            parts.Add(c.IsConnected ? "Conectado" : "Desconectado");

            _controllers.Add(new ExpandedItem
            {
                Name = c.Name,
                BatteryLevel = c.BatteryLevel,
                IsConnected = c.IsConnected,
                Details = string.Join(" · ", parts)
            });
        }
        UpdateSystemInfo();
    }

    private void UpdateSystemInfo()
    {
        var proc = System.Diagnostics.Process.GetCurrentProcess();
        var ram = proc.WorkingSet64 / 1024.0 / 1024.0;
        SystemInfoText.Text = $"NovaPad Overlay · RAM: {ram:F1} MB · PID: {proc.Id}";
    }
}
