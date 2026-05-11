using System.Windows.Controls;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class RGBView : UserControl
{
    public RGBView(RGBViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
