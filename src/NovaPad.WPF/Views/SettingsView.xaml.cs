using System.Windows.Controls;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class SettingsView : UserControl
{
    public SettingsView(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
