using McTools.Xrm.Connection;
using System;
using System.Collections.Generic;

namespace err403.SolutionManagment.AppCode
{
    public class TargetOrganizationsEventArgs : EventArgs
    {
        public List<ConnectionDetail> TargetOrganizations { get; set; }
    }
}