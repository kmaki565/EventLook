using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EventLook.Model;

public abstract class FilterBase : Monitorable
{
    private CollectionViewSource cvs;
    /// <summary>
    /// Injects the collection view source to the class. This must be called as soon as CVS is populated in the view.
    /// </summary>
    /// <param name="cvs"></param>
    public void SetCvs(CollectionViewSource cvs)
    {
        this.cvs = cvs;
    }
    /// <summary>
    /// Refreshes filter UI (e.g. populate filter items in a drop down) by loaded events,
    /// If carryOver is true, it'll try to carry over filters user specified (except newly populated checkboxes).
    /// Otherwise all filters will be cleared (e.g. checkboxes all checked).
    /// </summary>
    /// <param name="events">Loaded event items</param>
    public virtual void Refresh(IEnumerable<EventItem> events, bool carryOver) { }

    /// <summary>
    /// Removes filter and restores the default UI (to be called by "Reset filter" button).
    /// </summary>
    public abstract void Reset();

    /// <summary>
    /// Applies filter when the user operates the filter UI.
    /// </summary>
    public virtual void Apply()
    {
        RemoveFilter();
        AddFilter();
    }

    private bool isFilterAdded = false;
    protected void AddFilter()
    {
        if (isFilterAdded || cvs == null) return;

        SaveSortDescription();
        cvs.Filter += DoFilter;
        RestoreSortDescription();

        isFilterAdded = true;
        FireFilterUpdated(EventArgs.Empty);
    }
    protected void RemoveFilter()
    {
        if (!isFilterAdded || cvs == null) return;

        SaveSortDescription();
        cvs.Filter -= DoFilter;
        RestoreSortDescription();

        isFilterAdded = false;
        FireFilterUpdated(EventArgs.Empty);
    }
    protected void DoFilter(object sender, FilterEventArgs e)
    {
        // Set false if the event does not match to the filter criteria.
        // When using multiple filters, do not explicitly set anything to true.

        if (!(e.Item is EventItem evt))
            e.Accepted = false;
        else if (!IsFilterMatched(evt))
            e.Accepted = false;
    }
    /// <summary>
    /// Returns true if the given event item should be displayed, or false if the event should be filtered out.
    /// </summary>
    protected abstract bool IsFilterMatched(EventItem evt);

    /// <summary>
    /// Event that fires when a new filter is applied.
    /// </summary>
    public event EventHandler FilterUpdated;
    protected virtual void FireFilterUpdated(EventArgs e)
    {
        FilterUpdated?.Invoke(this, e);
    }

    private SortDescription sortDesc;
    /// <summary>
    /// Saves the first sort description of the collection view, 
    /// assuming multiple descriptions cannot be specified from the UI.
    /// </summary>
    private void SaveSortDescription()
    {
        sortDesc = cvs.View.SortDescriptions.FirstOrDefault();
    }
    /// <summary>
    /// Restores sort description of the view, since apparently 
    /// it's cleared when the filter event handler is added/removed.
    /// </summary>
    private void RestoreSortDescription()
    {
        if (sortDesc == default) return;
        
        cvs.View.SortDescriptions.Clear();
        cvs.View.SortDescriptions.Add(sortDesc);
    }
}
