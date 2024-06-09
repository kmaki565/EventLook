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
    private readonly ICollectionView _view;
    public DetailViewModel(EventItem eventItem, ICollectionView view)
    {
        Event = eventItem;
        _view = view;
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
        return _view.CurrentItem != null && _view.CurrentPosition > 0;
    }
    private bool IsNotLast()
    {
        return _view.CurrentItem != null && _view.CurrentPosition < _view.Cast<object>().Count() - 1;
    }
    private void Move(bool isUp)
    {
        if (isUp && IsNotFirst())
            _view.MoveCurrentToPrevious();
        else if (!isUp && IsNotLast())
            _view.MoveCurrentToNext();
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
