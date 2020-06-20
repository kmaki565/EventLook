﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLook.Model
{
    public class Range
    {
        public string Text { get; set; }
        public int DaysFromNow { get; set; }
        public bool IsCustom { get; set; }
    }
    public class RangeMgr
    {
        public RangeMgr()
        {
            Ranges = new ObservableCollection<Range>
            {
                new Range() { Text = "Last 1 day", DaysFromNow = 1, IsCustom = false },
                new Range() { Text = "Last 3 days", DaysFromNow = 3, IsCustom = false },
                new Range() { Text = "Last 7 days", DaysFromNow = 7, IsCustom = false },
                new Range() { Text = "Last 30 days", DaysFromNow = 30, IsCustom = false },
                new Range() { Text = "Custom range", DaysFromNow = 0, IsCustom = true },
            };
        }
        public ObservableCollection<Range> Ranges { get; set; }
    }
}
