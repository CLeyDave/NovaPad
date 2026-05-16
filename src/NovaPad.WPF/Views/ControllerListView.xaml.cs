using System.Windows.Controls;
using System.Windows.Input;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class ControllerListView : UserControl
{
    public ControllerListView(ControllerListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnRenameTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb || tb.DataContext is not ControllerItem item) return;
        var vm = DataContext as ControllerListViewModel;

        if (e.Key == Key.Enter && vm?.SaveRenameControllerCommand.CanExecute(item) == true)
            vm.SaveRenameControllerCommand.Execute(item);
        else if (e.Key == Key.Escape && vm?.CancelRenameControllerCommand.CanExecute(item) == true)
            vm.CancelRenameControllerCommand.Execute(item);
    }
}
