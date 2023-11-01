using EventLook.View;
using System.Windows.Data;

namespace EventLook.ViewModel;

/// <summary>
/// This is a simple data class used to transport a reference to the CollectionViewSource
/// from the view to the view model.
/// </summary>
public class ViewCollectionViewSourceMessageToken
{
    public CollectionViewSource CVS { get; set; }
}
/// <summary>
/// This is a simple data class used to transport name of a dropped file from the view to the view model.
/// </summary>
public class FileToBeProcessedMessageToken
{
    public string FilePath { get; set; }
}
public class DetailWindowServiceMessageToken
{
    public ShowWindowService<DetailWindow, DetailViewModel> DetailWindowService { get; set; }
}
public class LogPickerWindowServiceMessageToken
{
    public ShowWindowService<LogPickerWindow, LogPickerViewModel> LogPickerWindowService { get; set; }
}
public class SettingsWindowServiceMessageToken
{
    public ShowWindowService<SettingsWindow, SettingsViewModel> SettingsWindowService { get; set; }
}