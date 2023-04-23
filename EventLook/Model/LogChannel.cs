using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model;

public class LogChannel
{
    public string Path { get; set; }
    public bool HasEvent { get; set; } = true;
    public EventLogConfiguration Config { get; set; }
}
