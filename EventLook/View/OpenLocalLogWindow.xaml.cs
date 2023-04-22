using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EventLook.View
{
    /// <summary>
    /// Interaction logic for OpenLocalLogWindow.xaml
    /// </summary>
    public partial class OpenLocalLogWindow : Window
    {
        public OpenLocalLogWindow()
        {
            InitializeComponent();
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
}
