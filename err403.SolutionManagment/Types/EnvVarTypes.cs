using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace err403.SolutionManagment.Forms
{
    public class EnvVarEditRequestedEventArgs : EventArgs
    {
        public string DisplayName { get; set; }
        public string SchemaName { get; set; }
        public string TypeName { get; set; }
        public string SourceValue { get; set; }
        public Entity Definition { get; set; }
        public ListViewItem Item { get; set; }
    }

    public class EnvVarEditSaveEventArgs : EventArgs
    {
        public Dictionary<ConnectionDetail, string> ChangedValues { get; set; } = new Dictionary<ConnectionDetail, string>();
        public ListViewItem Item { get; set; }
    }

    public class EnvVarTransferRequestedEventArgs : EventArgs
    {
        public List<EnvVarTransferItem> Items { get; set; } = new List<EnvVarTransferItem>();
    }

    public class EnvVarTransferItem
    {
        public string DisplayName { get; set; }
        public string SchemaName { get; set; }
        public string TypeName { get; set; }
        public string SourceValue { get; set; }
        public Entity Definition { get; set; }
        public ListViewItem Item { get; set; }
    }

    public class TargetVariableInfo
    {
        public ConnectionDetail Detail { get; set; }
        public string Value { get; set; }
        public bool Exists { get; set; }
    }
}
