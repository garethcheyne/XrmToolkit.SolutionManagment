using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace err403.SolutionManagment.Services
{
    /// <summary>
    /// Checks for new connection references in solutions being transferred.
    /// Warns if targets don't have matching connection references configured.
    /// </summary>
    public static class ConnectionRefCheckService
    {
        public class ConnectionRefWarning
        {
            [JsonProperty("solutionName")] public string SolutionName { get; set; }
            [JsonProperty("connectionRefName")] public string ConnectionRefName { get; set; }
            [JsonProperty("targetName")] public string TargetName { get; set; }
            [JsonProperty("message")] public string Message { get; set; }
        }

        public static List<ConnectionRefWarning> Check(
            IOrganizationService sourceService,
            List<System.Guid> solutionIds,
            List<ConnectionDetail> targets)
        {
            var warnings = new List<ConnectionRefWarning>();

            // Get connection references from source solutions
            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid", "solutionid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 10016), // Connection Reference
                        new ConditionExpression("solutionid", ConditionOperator.In, solutionIds.Select(id => (object)id).ToArray())
                    }
                }
            };

            var components = sourceService.RetrieveMultiple(query).Entities;
            if (components.Count == 0) return warnings;

            var connRefIds = components.Select(c => c.GetAttributeValue<System.Guid>("objectid")).Distinct().ToList();

            // Get connection reference details
            foreach (var connRefId in connRefIds)
            {
                try
                {
                    var connRef = sourceService.Retrieve("connectionreference", connRefId,
                        new ColumnSet("connectionreferencedisplayname", "connectionreferencelogicalname"));
                    var displayName = connRef.GetAttributeValue<string>("connectionreferencedisplayname") ?? connRefId.ToString();

                    // Check each target
                    foreach (var target in targets)
                    {
                        var targetSvc = target.GetCrmServiceClient();
                        var targetQuery = new QueryExpression("connectionreference")
                        {
                            ColumnSet = new ColumnSet("connectionreferenceid"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("connectionreferencelogicalname", ConditionOperator.Equal,
                                        connRef.GetAttributeValue<string>("connectionreferencelogicalname"))
                                }
                            }
                        };

                        var targetRefs = targetSvc.RetrieveMultiple(targetQuery).Entities;
                        if (targetRefs.Count == 0)
                        {
                            warnings.Add(new ConnectionRefWarning
                            {
                                ConnectionRefName = displayName,
                                TargetName = target.ConnectionName,
                                Message = $"Connection reference '{displayName}' not found on {target.ConnectionName}"
                            });
                        }
                    }
                }
                catch { /* best effort */ }
            }

            return warnings;
        }
    }
}
