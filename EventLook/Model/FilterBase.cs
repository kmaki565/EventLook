using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EventLook.Model
{
    public abstract class FilterBase
    {
        /// <summary>
        /// Initializes filter UI (e.g. populate filter items in a drop down) after loading events.
        /// </summary>
        /// <param name="events">Loaded event items</param>
        public abstract void Init(IEnumerable<EventItem> events);

        /// <summary>
        /// Removes filter and clear the UI before loading events.
        /// </summary>
        /// <param name="cvs"></param>
        public abstract void Clear(CollectionViewSource cvs);

        /// <summary>
        /// Removes filter and restores the default UI for the Reset button.
        /// </summary>
        /// <param name="cvs"></param>
        public abstract void Reset(CollectionViewSource cvs);

        /// <summary>
        /// Applies filter when the user operates the filter UI.
        /// </summary>
        /// <param name="cvs"></param>
        public virtual void Apply(CollectionViewSource cvs)
        {
            RemoveFilter(cvs);
            AddFilter(cvs);
        }

        private bool isFilterAdded = false;
        protected void AddFilter(CollectionViewSource cvs)
        {
            if (isFilterAdded) return;

            cvs.Filter += DoFilter;
            isFilterAdded = true;
        }
        protected void RemoveFilter(CollectionViewSource cvs)
        {
            if (!isFilterAdded) return;

            cvs.Filter -= DoFilter;
            isFilterAdded = false;
        }
        protected abstract void DoFilter(object sender, FilterEventArgs e);
    }
}
