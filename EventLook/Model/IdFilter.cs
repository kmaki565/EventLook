using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EventLook.Model;

public class IdFilter : FilterBase
{
    public IdFilter()
    {
        IdFilterNum = null;
    }

    //TODO: May want to support comma-separated multiple IDs in the input
    private int? idFilterNum;
    public int? IdFilterNum
    {
        get { return idFilterNum; }
        set
        {
            if (value == idFilterNum)
                return;

            idFilterNum = value;
            NotifyPropertyChanged();

            Apply();
        }
    }

    public override void Refresh(IEnumerable<EventItem> events, bool reset)
    {
        if (reset)
            Clear();
    }
    public override void Clear()
    {
        IdFilterNum = null;
    }

    protected override bool IsFilterMatched(EventItem evt)
    {
        if (idFilterNum.HasValue)
            return evt.Record.Id == IdFilterNum.Value;
        else
            return true;    // No filter specified.
    }
}
