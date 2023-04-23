using EventLook.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace EventLook.View;

/// <summary>
/// Interaction logic for LogPickerWindow.xaml
/// </summary>
public partial class LogPickerWindow : Window
{
    public LogPickerWindow()
    {
        InitializeComponent();
        ContentRendered += (s, e) => { ((LogPickerViewModel)DataContext).OnLoaded(); };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = listBox1.SelectedItem != null;
        Close();
    }

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OkButton_Click(sender, e);
    }
}
