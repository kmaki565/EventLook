using System.Windows;
using System.Windows.Input;

namespace EventLook.View;

/// <summary>
/// Interaction logic for DetailWindow.xaml
/// </summary>
public partial class DetailWindow : Window
{
    public DetailWindow()
    {
        InitializeComponent();
        this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
    }

    private void HandleEsc(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}
