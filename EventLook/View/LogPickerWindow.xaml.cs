using EventLook.ViewModel;
using System.Data;
using System;
using System.Windows;
using System.Windows.Controls;
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
        DialogResult = dataGrid1.SelectedItem != null;
        Close();
    }
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OkButton_Click(sender, e);
    }

    private void dataGrid1_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && dataGrid1.SelectedItem != null)
        {
            e.Handled = true;
            OkButton_Click(sender, e);
        }
    }
}
