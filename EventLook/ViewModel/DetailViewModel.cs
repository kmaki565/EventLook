using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventLook.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        _view.CollectionChanged += OnCollectionChanged;

        Event = (EventItem)_view.CurrentItem;
        // Save my position in case multiple detail windows are open.
        position = _view.CurrentPosition;

        UpCommand = new RelayCommand(() => Move(isUp: true), CanMoveUp);
        DownCommand = new RelayCommand(() => Move(isUp: false), CanMoveDown);
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

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        // CollectionChanged event is raised too many times when log source is changed.
        // Handle Reset event only hoping to cover most cases.
        if (e.Action == NotifyCollectionChangedAction.Reset)
            UpdateCanExecute();
    }

    private bool CanMoveUp()
    {
        //TODO: Check if the current displayed item exists in the view.
        if (_view.CurrentItem is EventItem ev && Event?.LogSource == ev.LogSource && position > 0 && position < _view.Cast<object>().Count())
            return true;
        else
            return false;
    }
    private bool CanMoveDown()
    {
        if (_view.CurrentItem is EventItem ev && Event?.LogSource == ev.LogSource && position < _view.Cast<object>().Count() - 1)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Updates the item to display by moving up/down in the view based on the position.
    /// </summary>
    /// <param name="isUp"></param>
    private void Move(bool isUp)
    {
        // Limitation: the position and item may not match when filter is updated.
        if (isUp)
            _view.MoveCurrentToPosition(--position);
        else 
            _view.MoveCurrentToPosition(++position);

        Event = (EventItem)_view.CurrentItem;
        UpdateCanExecute();
    }

    private void UpdateCanExecute()
    {
        UpCommand?.NotifyCanExecuteChanged();
        DownCommand?.NotifyCanExecuteChanged();
    }
}
