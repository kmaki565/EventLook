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
        // CollectionChanged (Add) event is raised too many times when log source is changed.
        // Handle Reset event only hoping to cover most cases.
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UpdatePosition();
            UpdateCanExecute();
        }
    }
    private void UpdateCanExecute()
    {
        UpCommand?.NotifyCanExecuteChanged();
        DownCommand?.NotifyCanExecuteChanged();
    }

    private bool CanMoveUp()
    {
        return IsEventInView() && !IsFirst();
    }
    private bool CanMoveDown()
    {
        return IsEventInView() && !IsLast();
    }

    private bool IsEventInView()
    {
        return _view.Cast<EventItem>().Any(x => x.LogSource == Event?.LogSource && x.Record.RecordId == Event?.Record.RecordId);
    }
    /// <summary>
    /// Checks if the displayed event is the first item (or before) in the view.
    /// </summary>
    /// <returns></returns>
    private bool IsFirst()
    {
        return position <= 0;
    }
    /// <summary>
    /// Checks if the displayed event is the last item (or beyond) in the view.
    /// </summary>
    /// <returns></returns>
    private bool IsLast()
    {
        return position >= _view.Cast<object>().Count() - 1;
    }

    /// <summary>
    /// Updates the item to display by moving up/down in the view based on the position.
    /// </summary>
    /// <param name="isUp"></param>
    private void Move(bool isUp)
    {
        if (isUp && !IsFirst())
            _view.MoveCurrentToPosition(--position);
        else if (!isUp && !IsLast())
            _view.MoveCurrentToPosition(++position);
        else
            return;

        Event = (EventItem)_view.CurrentItem;
        UpdateCanExecute();
    }
    /// <summary>
    /// Updates position in the object in case the view has changed (sort, filter, etc.).
    /// </summary>
    private void UpdatePosition()
    {
        if (IsEventInView())
        {
            // Apparently we can't get the position directly from the view.
            // This may cause issue when multiple detail windows are open.
            position = _view.CurrentPosition;
        }
    }
}
