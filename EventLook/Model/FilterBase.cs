using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EventLook.Model
{
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
        /// trying to carry over filters user specified.
        /// </summary>
        /// <param name="events">Loaded event items</param>
        public virtual void Refresh(IEnumerable<EventItem> events) { }

        /// <summary>
        /// Removes filter and restores the default UI for the Reset button.
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

            cvs.Filter += DoFilter;
            isFilterAdded = true;
            FireFilterUpdated(EventArgs.Empty);
        }
        protected void RemoveFilter()
        {
            if (!isFilterAdded || cvs == null) return;

            cvs.Filter -= DoFilter;
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
    }
}
