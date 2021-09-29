using EventLook.View;
using EventLook.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventLook
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Here we send a message which is caught by the view model.  The message contains a reference
            // to the CollectionViewSource which is instantiated when the view is instantiated (before the view model).
            Messenger.Default.Send(new ViewCollectionViewSourceMessageToken() { CVS = (CollectionViewSource)(this.Resources["X_CVS"]) });
            
            var showWindowService = new ShowWindowService<DetailWindow, DetailViewModel>(){ Owner = this };
            Messenger.Default.Send(new DetailWindowMessageToken() { ShowWindowService = showWindowService });

            ContentRendered += (s, e) => { ((MainViewModel)DataContext).OnLoaded(); };

            ProcessCommandLine();
        }

        private void ProcessCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                var fileName = args[1];
                if (File.Exists(fileName))
                {
                    var extension = System.IO.Path.GetExtension(fileName);
                    if (extension == ".evtx")
                    {
                        //TODO: Select the file in the log source UI
                        Messenger.Default.Send(new FileToBeProcessedMessageToken() { FilePath = fileName });
                    }
                }
            }
        }

        private void OnPreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }
        private void OnDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                foreach (string uriString in files)
                {
                    // Pass the file name to the MainViewModel
                    Messenger.Default.Send(new FileToBeProcessedMessageToken() { FilePath = uriString });
                }
            }
        }
    }
}
