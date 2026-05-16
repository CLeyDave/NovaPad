using System;
using System.Windows;
using System.Windows.Controls;
using NovaPad.WPF.Helpers;
using NovaPad.WPF.ViewModels;

namespace NovaPad.WPF.Views;

public partial class ControllerDetailView : UserControl
{
    private bool _webViewInitialized;

    public ControllerDetailView(ControllerDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_webViewInitialized) return;

        try
        {
            // Online (e7d.io)
            await OnlineWebView.EnsureCoreWebView2Async();
            if (OnlineWebView.CoreWebView2 != null)
            {
                OnlineWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                OnlineWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                OnlineWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                OnlineWebView.Source = new Uri("https://gamepad.e7d.io/?triggers=meter&background=transparent&color=black&type=ds4");
            }

            // Local (PS layout)
            await LocalDosWebView.EnsureCoreWebView2Async();
            if (LocalDosWebView.CoreWebView2 != null)
            {
                LocalDosWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                LocalDosWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                LocalDosWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                LocalDosWebView.NavigateToString(GamepadTestPagePS.GetHtml());
            }

            _webViewInitialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebView2] Error: {ex.Message}");
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            OnlineWebView?.Dispose();
            LocalDosWebView?.Dispose();
            _webViewInitialized = false;
        }
        catch { }
    }
}
