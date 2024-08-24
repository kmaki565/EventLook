using AboutBoxWpf;
using CommunityToolkit.Mvvm.Messaging;
using EventLook.Model;
using EventLook.Properties;
using EventLook.ViewModel;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace EventLook.View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var mainViewModel = new MainViewModel(new DataService());
        DataContext = mainViewModel;

        // Here we send a message which is caught by the view model.  The message contains a reference
        // to the CollectionViewSource which is instantiated when the view is instantiated (before the view model).
        WeakReferenceMessenger.Default.Send(new ViewCollectionViewSourceMessageToken() { CVS = (CollectionViewSource)(this.Resources["X_CVS"]) });
        
        var detailWindowService = new ShowWindowService<DetailWindow, DetailViewModel>(){ Owner = this };
        WeakReferenceMessenger.Default.Send(new DetailWindowServiceMessageToken() { DetailWindowService = detailWindowService });
        var logPickerWindowService = new ShowWindowService<LogPickerWindow, LogPickerViewModel>() { Owner = this };
        WeakReferenceMessenger.Default.Send(new LogPickerWindowServiceMessageToken() { LogPickerWindowService = logPickerWindowService });
        var settingsWindowService = new ShowWindowService<SettingsWindow, SettingsViewModel>() { Owner = this };
        WeakReferenceMessenger.Default.Send(new SettingsWindowServiceMessageToken() { SettingsWindowService = settingsWindowService });

        // It looks like the expander needs expanded once to check "Select All" even if all other checkboxes are checked.
        Ex1.IsExpanded = true;
        ContentRendered += (_, _) =>
        {
            Ex1.IsExpanded = false;
            mainViewModel.OnLoaded();
        };
        mainViewModel.Refreshing += OnRefreshing;
        mainViewModel.Refreshed += OnRefreshed;

        ProcessCommandLine();
    }

	private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		Settings.Default.Save();
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
                    WeakReferenceMessenger.Default.Send(new FileToBeProcessedMessageToken() { FilePath = fileName });
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
                WeakReferenceMessenger.Default.Send(new FileToBeProcessedMessageToken() { FilePath = uriString });
            }
        }
    }
    private async void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        if (isRefreshing)
            return;

        if (sender is DataGrid dataGrid)
        {
            // Without a delay it may fail to scroll to the selected event when resetting filters.
            await Task.Delay(1);
            if (dataGrid.SelectedItem != null)
                dataGrid.ScrollIntoView(dataGrid.SelectedItem);
        }
    }
    private void CheckTruncate_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid1.SelectedItem != null)
            dataGrid1.ScrollIntoView(dataGrid1.SelectedItem);
    }
    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            if (Ex1.IsExpanded == false)
            {
                Ex1.IsExpanded = true;
                await Task.Delay(1);
            }
            textBoxMsgFilter.Focus();
        }
    }
    private bool isRefreshing = false;
    private void OnRefreshing()
    {
        isRefreshing = true;

        // When an older event is selected, refreshing entire events causes the DataGrid to keep auto-scrolling.
        // This is to workaround the issue, but needs to be revisited.
        ScrollToTop();
    }
    private void OnRefreshed()
    {
        isRefreshing = false;

        // If focus within MainWindow is on these elements (or no focus), set focus to the DataGrid.
        // Focus will be somehow on DataGridCell when user clicks a row in DataGrid during refresh.

        IInputElement focusedElement = FocusManager.GetFocusedElement(this);
        if ((focusedElement is DataGridCell && Keyboard.FocusedElement is not MenuItem 
            || focusedElement == null && Keyboard.FocusedElement == this)
            && dataGrid1.Items.Count > 0)
        {
            // After refresh, SelectedIndex is -1 unless you click the dataGrid during refresh.
            SelectRow.SelectRowByIndex(dataGrid1, dataGrid1.SelectedIndex < 0 ? 0 : dataGrid1.SelectedIndex);
        }
    }

    private void ScrollToTop()
    {
        dataGrid1.SelectedIndex = 0;
        if (dataGrid1.SelectedItem != null)
            dataGrid1.ScrollIntoView(dataGrid1.SelectedItem);
    }

    private void MenuItem_About_Click(object sender, RoutedEventArgs e)
    {
        AboutControlView about = new();
        AboutControlViewModel vm = (AboutControlViewModel)about.FindResource("ViewModel");
        vm.IsSemanticVersioning = true;
        vm.VersionAppendix = $"({BuildInfo.GetBuildDate():d})";

        var packageType = BuildInfo.GetPackageType();
        vm.PackageInfoText =
            packageType == BuildInfo.PackageType.Store ? "For Microsoft Store" :
            packageType == BuildInfo.PackageType.DevPackage ? "Developer package" :
            "Unpackaged app";

        vm.ApplicationLogo = new BitmapImage(new Uri("pack://application:,,,/Asset/favicon.ico"));
        vm.PublisherLogo = new BitmapImage(new Uri("pack://application:,,,/Asset/favicon.ico"));
        vm.HyperlinkText = "https://github.com/kmaki565/EventLook";

        vm.Window.Content = about;
        vm.Window.Owner = this; // To make the child window always on top of this window.
        vm.Window.ShowDialog();
    }
}
