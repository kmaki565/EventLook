using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventLook.Model;
using EventLook.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EventLook.ViewModel;
public class SettingsViewModel : ObservableObject
{
    private readonly IShowWindowService<LogPickerViewModel> _logPickerWindowService;

    public SettingsViewModel(IShowWindowService<LogPickerViewModel> logPickerWindowService)
    {
        _logPickerWindowService = logPickerWindowService;
        StartupLogSources = new ObservableCollection<string>(Properties.Settings.Default.StartupLogSources);

        AddCommand = new RelayCommand(AddLogSources);
        RemoveCommand = new RelayCommand(RemoveLogSources);
        ApplyCommand = new RelayCommand(Apply);
    }

    public ObservableCollection<string> StartupLogSources { get; set; }
    public string SelectedLogName { get; set; }

    public ICommand AddCommand { get; private set; }
    public ICommand RemoveCommand { get; private set; }
    public ICommand ApplyCommand { get; private set; }

    private void AddLogSources()
    {
        var openLogVm = new LogPickerViewModel();
        bool? ret = _logPickerWindowService.ShowDialog(openLogVm);
        if (ret == true && !string.IsNullOrEmpty(openLogVm.SelectedChannel?.Path))
        {
            StartupLogSources.Add(openLogVm.SelectedChannel.Path);
        }
    }
    private void RemoveLogSources()
    {
        if (SelectedLogName != null)
        {
            StartupLogSources.Remove(SelectedLogName);
        }
    }
    private void Apply()
    {
        Properties.Settings.Default.StartupLogSources = StartupLogSources.ToList();
        Properties.Settings.Default.Save();
    }
}
