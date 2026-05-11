using System.Windows.Controls;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class BatteryView : UserControl
{
    public BatteryView(BatteryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
