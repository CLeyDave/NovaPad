using System.Windows.Controls;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class PaginaOverlay : UserControl
{
    private readonly AdminOverlayVm _vm;

    public PaginaOverlay(AdminOverlayVm vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += (_, _) => _vm.UpdateRunningState();
    }
}
