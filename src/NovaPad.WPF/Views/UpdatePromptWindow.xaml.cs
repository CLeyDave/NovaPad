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
        VersionText.Text = $"NovaPad {newVersion}";
    }

    private void BtnDownloadInstall_Click(object sender, RoutedEventArgs e)
    {
        Choice = UpdateChoice.DownloadInstall;
        DialogResult = true;
        Close();
    }

    private void BtnDownload_Click(object sender, RoutedEventArgs e)
    {
        Choice = UpdateChoice.DownloadOnly;
        DialogResult = true;
        Close();
    }

    private void BtnLater_Click(object sender, RoutedEventArgs e)
    {
        Choice = UpdateChoice.Later;
        DialogResult = false;
        Close();
    }
}
