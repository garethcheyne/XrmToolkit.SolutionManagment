using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace err403.SolutionManagment.Forms
{
    public class SettingSyncRequestedEventArgs : EventArgs
    {
        public List<SettingSyncItemLegacy> Settings { get; set; } = new List<SettingSyncItemLegacy>();
    }

    public class SettingSyncItemLegacy
    {
        public Entity Definition { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string SourceValue { get; set; }
    }
}
