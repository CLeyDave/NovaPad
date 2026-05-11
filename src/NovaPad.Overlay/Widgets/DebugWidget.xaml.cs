using System.Windows.Controls;

namespace NovaPad.Overlay.Widgets;

public partial class DebugWidget : UserControl
{
    private int _frameCount;
    private double _lastFpsUpdate;
    private double _currentFps;

    public DebugWidget()
    {
        InitializeComponent();
    }

    public void Tick(double timestamp)
    {
        _frameCount++;

        if (timestamp - _lastFpsUpdate >= 1.0)
        {
            _currentFps = _frameCount / (timestamp - _lastFpsUpdate);
            _frameCount = 0;
            _lastFpsUpdate = timestamp;

            Dispatcher.Invoke(() =>
            {
                FpsText.Text = $"FPS: {_currentFps:F0}";
                RenderText.Text = $"Frame: {_frameCount}";
            });
        }
    }

    public void SetLatency(double ms)
    {
        Dispatcher.Invoke(() =>
        {
            LatencyText.Text = $"Input: {ms:F1} ms";
        });
    }
}
