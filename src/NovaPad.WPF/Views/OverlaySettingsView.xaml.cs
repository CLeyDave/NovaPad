using System.Windows.Controls;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class OverlaySettingsView : UserControl
{
    public OverlaySettingsView(OverlaySettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
