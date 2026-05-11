using System.Windows;
using System.Windows.Controls;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class ControllerDetailView : UserControl
{
    private readonly ControllerDetailViewModel _vm;

    public ControllerDetailView(ControllerDetailViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await GamepadWebView.EnsureCoreWebView2Async();
        GamepadWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        GamepadWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        GamepadWebView.Source = new Uri("https://gamepad.e7d.io/?triggers=meter&background=transparent&color=black&type=ds4");
    }
}
