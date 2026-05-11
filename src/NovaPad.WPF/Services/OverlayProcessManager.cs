using System.Diagnostics;
using System.IO;

namespace NovaPad.WPF.Services;

public class OverlayProcessManager : IDisposable
{
    private Process? _process;
    private readonly OverlayClient _overlayClient;
    private bool _isRunning;

    public OverlayClient Client => _overlayClient;
    public bool IsRunning => _isRunning;

    public OverlayProcessManager(OverlayClient overlayClient)
    {
        _overlayClient = overlayClient;
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        var exePath = Path.Combine(
            AppContext.BaseDirectory,
            "NovaPad.Overlay.exe");

        if (!File.Exists(exePath))
        {
            Debug.WriteLine($"Overlay not found: {exePath}");
            return;
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
        _process.Exited += (_, _) => _isRunning = false;
        _process.Start();
        _isRunning = true;

        await Task.Delay(500);
        await _overlayClient.ConnectAsync();
    }

    public void Stop()
    {
        if (_process != null && !_process.HasExited)
        {
            _process.Kill();
            _process.Dispose();
            _process = null;
        }
        _isRunning = false;
        _overlayClient.Dispose();
    }

    public void Dispose()
    {
        Stop();
    }
}
