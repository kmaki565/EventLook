﻿using EventLook.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.ViewModel;

public class DetailViewModel(EventItem eventItem, LogSource logSource)
{
    public EventItem Event { get; set; } = eventItem;
    public string EventXml { get => Event.GetXml(logSource) ?? "Could not retrieve XML."; }
}
