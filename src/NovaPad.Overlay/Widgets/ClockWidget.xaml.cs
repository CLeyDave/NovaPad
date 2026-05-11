using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NovaPad.Overlay.Widgets;

public partial class ClockWidget : UserControl
{
    private readonly DispatcherTimer _timer;

    public ClockWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateTime();
        _timer.Start();
        UpdateTime();
    }

    private void UpdateTime()
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
    }
}
