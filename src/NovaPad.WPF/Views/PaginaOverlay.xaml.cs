using System.Windows.Controls;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class PaginaOverlay : UserControl
{
    public PaginaOverlay(AdminOverlayVm vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
