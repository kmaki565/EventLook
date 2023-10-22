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

        AddCommand = new RelayCommand(AddLogSource);
        RemoveCommand = new RelayCommand(RemoveLogSource);
        UpCommand = new RelayCommand(MoveUpLogSource);
        DownCommand = new RelayCommand(MoveDownLogSource);
        RestoreDefaultCommand = new RelayCommand(RestoreDefaultLogSources);
        ApplyCommand = new RelayCommand(Apply);
    }

    public ObservableCollection<string> StartupLogSources { get; set; }
    public string SelectedLogName { get; set; }

    public ICommand AddCommand { get; private set; }
    public ICommand RemoveCommand { get; private set; }
    public ICommand UpCommand { get; private set; }
    public ICommand DownCommand { get; private set; }
    public ICommand RestoreDefaultCommand { get; private set; }
    public ICommand ApplyCommand { get; private set; }

    private void AddLogSource()
    {
        var openLogVm = new LogPickerViewModel();
        bool? ret = _logPickerWindowService.ShowDialog(openLogVm);
        if (ret == true && !string.IsNullOrEmpty(openLogVm.SelectedChannel?.Path))
        {
            StartupLogSources.Add(openLogVm.SelectedChannel.Path);
        }
    }
    private void RemoveLogSource()
    {
        if (SelectedLogName != null)
        {
            StartupLogSources.Remove(SelectedLogName);
        }
    }
    private void MoveUpLogSource()
    {
        if (SelectedLogName != null)
        {
            int index = StartupLogSources.IndexOf(SelectedLogName);
            if (index > 0)
            {
                StartupLogSources.Move(index, index - 1);
            }
        }
    }
    private void MoveDownLogSource()
    {
        if (SelectedLogName != null)
        {
            int index = StartupLogSources.IndexOf(SelectedLogName);
            if (index < StartupLogSources.Count - 1)
            {
                StartupLogSources.Move(index, index + 1);
            }
        }
    }
    private void RestoreDefaultLogSources()
    {
        StartupLogSources.Clear();
        foreach (string log in LogSourceMgr.defaultLogSources)
        {
            StartupLogSources.Add(log);
        }
    }
    private void Apply()
    {
        Properties.Settings.Default.StartupLogSources = StartupLogSources.ToList();
        Properties.Settings.Default.Save();
    }
}
