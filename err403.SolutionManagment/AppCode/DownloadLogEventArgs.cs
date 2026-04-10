using Microsoft.Xrm.Sdk;
using System;

namespace err403.SolutionManagment.AppCode
{
    public class DownloadLogEventArgs : EventArgs
    {
        public Guid ImportJobId { get; set; }

        public IOrganizationService Service { get; set; }
    }
}