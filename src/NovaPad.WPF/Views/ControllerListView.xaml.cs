using System.Windows.Controls;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class ControllerListView : UserControl
{
    public ControllerListView(ControllerListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
