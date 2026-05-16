using System.Windows;

namespace NovaPad.WPF.Views;

public enum UpdateChoice
{
    DownloadInstall,
    DownloadOnly,
    Later
}

public partial class UpdatePromptWindow
{
    public UpdateChoice Choice { get; private set; } = UpdateChoice.Later;

    public UpdatePromptWindow(string newVersion)
    {
        InitializeComponent();
        VersionText.Text = $"NovaPad {newVersion} esta disponible.";
    }

    private void BtnDownloadInstall_Click(object sender, RoutedEventArgs e)
    {
        Choice = UpdateChoice.DownloadInstall;
        Close();
    }

    private void BtnDownload_Click(object sender, RoutedEventArgs e)
    {
        Choice = UpdateChoice.DownloadOnly;
        Close();
    }

    private void BtnLater_Click(object sender, RoutedEventArgs e)
    {
        Choice = UpdateChoice.Later;
        Close();
    }
}
