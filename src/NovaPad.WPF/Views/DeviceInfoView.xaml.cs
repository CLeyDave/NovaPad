using System.Windows.Controls;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class DeviceInfoView : UserControl
{
    public DeviceInfoView(DeviceInfoViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
