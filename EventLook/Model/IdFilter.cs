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

            if (value == null)
                RemoveFilter();
            else
                Apply();
        }
    }

    public override void Reset()
    {
        IdFilterNum = null;
    }

    protected override bool IsFilterMatched(EventItem evt)
    {
        if (idFilterNum.HasValue)
            return evt.Record.Id == IdFilterNum.Value;
        else
            return true;    // Shouldn't come here - null means no filter specified.
    }
}
