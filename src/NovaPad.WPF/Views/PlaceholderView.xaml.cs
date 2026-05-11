using System.Windows.Controls;

namespace NovaPad.WPF.Views;

public partial class PlaceholderView : UserControl
{
    public PlaceholderView(string title)
    {
        InitializeComponent();
        TitleText.Text = title;
        DescText.Text = $"{title} — Próximamente en una actualización futura.";
    }
}
