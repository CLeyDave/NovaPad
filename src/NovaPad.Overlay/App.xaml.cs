using System.Windows;

namespace NovaPad.Overlay;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var window = new OverlayWindow();
        window.Show();
    }
}
