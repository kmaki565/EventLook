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
        /// Initializes filter UI (e.g. populate filter items in a drop down) after loading events.
        /// </summary>
        /// <param name="events">Loaded event items</param>
        public abstract void Init(IEnumerable<EventItem> events);

        /// <summary>
        /// Removes filter and clear the UI before loading events.
        /// </summary>
        public abstract void Clear();

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
        }
        protected void RemoveFilter()
        {
            if (!isFilterAdded || cvs == null) return;

            cvs.Filter -= DoFilter;
            isFilterAdded = false;
        }
        protected abstract void DoFilter(object sender, FilterEventArgs e);
    }
}
