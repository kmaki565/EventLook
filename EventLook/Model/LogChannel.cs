using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model
{
    public class LogChannel
    {
        public string Path { get; set; }
        public bool HasAnyEvent { get; set; } = true;
    }
}
