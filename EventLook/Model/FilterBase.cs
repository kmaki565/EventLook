using System;
using System.Collections.Generic;
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
        this.cvs.Filter += DoFilter;
    }
    /// <summary>
    /// Refreshes filter UI (e.g. populate filter items in a drop down) by loaded events,
    /// If reset is true, all filters will be cancelled (e.g. checkboxes all checked).
    /// Otherwise, it'll try to carry over filters user specified (except newly populated checkboxes).
    /// </summary>
    /// <param name="events">Loaded event items</param>
    public virtual void Refresh(IEnumerable<EventItem> events, bool reset) { }

    /// <summary>
    /// Removes filter, but keeps the filter items in the dropdown (if available).
    /// This is to be called by "Reset filter" button.
    /// </summary>
    public abstract void Clear();
    /// <summary>
    /// Removes filter and filter items in the dropdown (if available).
    /// </summary>
    public virtual void Reset()
    {
        Clear();
    }

    /// <summary>
    /// Applies filter when the user operates the filter UI.
    /// </summary>
    public void Apply()
    {
        if (cvs != null)
        {
            cvs.View.Refresh();
            FireFilterUpdated(EventArgs.Empty);
        }
    }

    protected void DoFilter(object sender, FilterEventArgs e)
    {
        // Set false if the event does not match to the filter criteria.
        // When using multiple filters, do not explicitly set anything to true.

        if (e.Item is not EventItem evt)
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
}
