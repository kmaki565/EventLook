using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model
{
    public class SourceFilterItem : INotifyPropertyChanged
    {
        private bool _selected;
        private string _name;

        public string Name
        {
            get => _name; set
            {
                _name = value;
                EmitChange(nameof(Name));
            }
        }
        public bool Selected
        {
            get => _selected; set
            {
                _selected = value;
                EmitChange(nameof(Selected));
            }
        }

        private void EmitChange(params string[] names)
        {
            if (PropertyChanged == null)
                return;
            foreach (var name in names)
                PropertyChanged(this,
                  new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler
                       PropertyChanged;
    }
}
