using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace err403.SolutionManagment.Forms
{
    public class FlowActivateRequestedEventArgs : EventArgs
    {
        public List<FlowActionItem> Flows { get; set; } = new List<FlowActionItem>();
        public bool Activate { get; set; }
    }

    public class FlowActionItem
    {
        public string FlowName { get; set; }
        public Guid WorkflowId { get; set; }
        public Entity Workflow { get; set; }
        public ListViewItem Item { get; set; }
    }

    public class FlowActionRequestedEventArgs : EventArgs
    {
        public List<FlowActionItem> Flows { get; set; } = new List<FlowActionItem>();
        public McTools.Xrm.Connection.ConnectionDetail TargetDetail { get; set; }
        public bool Activate { get; set; }
    }
}
