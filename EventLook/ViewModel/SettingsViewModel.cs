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

    public SettingsViewModel(IShowWindowService<LogPickerViewModel> logPickerWindowService, SettingsData originalSettings)
    {
        _logPickerWindowService = logPickerWindowService;

        // COPY the original settings for this view model.
        // Pick up non-file log sources only.
        LogSources = new ObservableCollection<LogSource>(originalSettings.LogSources.Where(x => x.PathType == PathType.LogName));
        Ranges = new List<Model.Range>(originalSettings.Ranges);
        SelectedRange = Ranges.FirstOrDefault(r => r.Text == originalSettings.SelectedRange.Text);

        AddCommand = new RelayCommand(AddLogSource);
        RemoveCommand = new RelayCommand(RemoveLogSource);
        UpCommand = new RelayCommand(MoveUpLogSource);
        DownCommand = new RelayCommand(MoveDownLogSource);
        RestoreDefaultCommand = new RelayCommand(RestoreDefaultLogSources);
    }

    public ObservableCollection<LogSource> LogSources { get; set; }
    public LogSource SelectedLogSource { get; set; }
    public IReadOnlyCollection<Model.Range> Ranges { get; set; }
    private Model.Range selectedRange;
    public Model.Range SelectedRange { get => selectedRange; set => SetProperty(ref selectedRange, value); }

    /// <summary>
    /// Returns the result of the settings dialog.
    /// </summary>
    /// <returns></returns>
    public SettingsData GetSettingsInfo()
    {
        return new SettingsData
        {
            LogSources = LogSources.ToList(),
            Ranges = Ranges.ToList(),
            SelectedRange = SelectedRange
        };
    }

    public ICommand AddCommand { get; private set; }
    public ICommand RemoveCommand { get; private set; }
    public ICommand UpCommand { get; private set; }
    public ICommand DownCommand { get; private set; }
    public ICommand RestoreDefaultCommand { get; private set; }

    private void AddLogSource()
    {
        var openLogVm = new LogPickerViewModel();
        bool? ret = _logPickerWindowService.ShowDialog(openLogVm);
        if (ret == true && !string.IsNullOrEmpty(openLogVm.SelectedChannel?.Path))
        {
            LogSources.Add(new LogSource(openLogVm.SelectedChannel.Path));
        }
    }
    private void RemoveLogSource()
    {
        if (SelectedLogSource != null)
        {
            LogSources.Remove(SelectedLogSource);
        }
    }
    private void MoveUpLogSource()
    {
        if (SelectedLogSource != null)
        {
            int index = LogSources.IndexOf(SelectedLogSource);
            if (index > 0)
            {
                LogSources.Move(index, index - 1);
            }
        }
    }
    private void MoveDownLogSource()
    {
        if (SelectedLogSource != null)
        {
            int index = LogSources.IndexOf(SelectedLogSource);
            if (index < LogSources.Count - 1)
            {
                LogSources.Move(index, index + 1);
            }
        }
    }
    private void RestoreDefaultLogSources()
    {
        LogSources.Clear();
        foreach (string log in LogSourceMgr.defaultLogSources)
        {
            LogSources.Add(new LogSource(log));
        }
    }
}
