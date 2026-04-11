using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace err403.SolutionManagment.Services
{
    /// <summary>
    /// Queries target environments for solution versions, env var values, etc.
    /// C# must do this because each target has its own CrmServiceClient auth.
    /// Returns JSON for React to display.
    /// </summary>
    public static class TargetQueryService
    {
        public static string GetTargetSolutions(ConnectionDetail target, List<string> uniqueNames)
        {
            if (uniqueNames == null || uniqueNames.Count == 0) return "[]";

            var svc = target.GetCrmServiceClient();
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("uniquename", "version", "ismanaged"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.In, uniqueNames.ToArray())
                    }
                }
            };

            var solutions = svc.RetrieveMultiple(query).Entities;
            return JsonConvert.SerializeObject(solutions.Select(s => new
            {
                uniquename = s.GetAttributeValue<string>("uniquename"),
                version = s.GetAttributeValue<string>("version"),
                ismanaged = s.GetAttributeValue<bool>("ismanaged")
            }));
        }
    }
}
