using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventLook.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.ViewModel;

public class DetailViewModel : ObservableObject
{
    /// <summary>
    /// VM for the detail window.
    /// Receives the main view of the events to walk through the events.
    /// </summary>
    private readonly ICollectionView _view;
    private int position;
    public DetailViewModel(ICollectionView view)
    {
        _view = view;
        Event = (EventItem)_view.CurrentItem;
        // Save my position in case multiple detail windows are open.
        position = _view.CurrentPosition;

        UpCommand = new RelayCommand(() => Move(isUp: true), IsNotFirst);
        DownCommand = new RelayCommand(() => Move(isUp: false), IsNotLast);
    }

    private EventItem _event;
    public EventItem Event
    {
        get => _event;
        set
        {
            if (value != _event)
            {
                _event = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EventXml));
            }
        }
    }
    public string EventXml { get => Event?.GetXml() ?? "Could not retrieve Event XML."; }
    public IRelayCommand UpCommand { get; private set; }
    public IRelayCommand DownCommand { get; private set; }

    private bool IsNotFirst()
    {
        return Event != null && position > 0;
    }
    private bool IsNotLast()
    {
        return Event != null && position < _view.Cast<object>().Count() - 1;
    }
    private void Move(bool isUp)
    {
        if (isUp && IsNotFirst())
            _view.MoveCurrentToPosition(--position);
        else if (!isUp && IsNotLast())
            _view.MoveCurrentToPosition(++position);
        else
            return;

        Event = (EventItem)_view.CurrentItem;
        UpdateCanExecute();
    }

    private void UpdateCanExecute()
    {
        UpCommand?.NotifyCanExecuteChanged();
        DownCommand?.NotifyCanExecuteChanged();
    }
}
