using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace err403.SolutionManagment.Services
{
    /// <summary>
    /// Activates/deactivates cloud flows on target environments via SetStateRequest.
    /// Pure execution — no UI. Returns JSON results for React to display.
    /// </summary>
    public static class FlowActivationService
    {
        public class FlowActionRequest
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("workflowId")] public string WorkflowId { get; set; }
        }

        public class FlowActionResult
        {
            public string FlowName { get; set; }
            public string TargetName { get; set; }
            public string TargetEnvironmentId { get; set; }
            public Guid TargetFlowId { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public bool IsConnectionRefError { get; set; }
        }

        public static List<FlowActionResult> Execute(
            List<FlowActionRequest> flows,
            ConnectionDetail targetDetail,
            bool activate)
        {
            var results = new List<FlowActionResult>();
            var svc = targetDetail.GetCrmServiceClient();

            foreach (var flow in flows)
            {
                var result = new FlowActionResult
                {
                    FlowName = flow.Name,
                    TargetName = targetDetail.ConnectionName,
                    TargetEnvironmentId = targetDetail.EnvironmentId
                };

                try
                {
                    var query = new QueryExpression("workflow")
                    {
                        ColumnSet = new ColumnSet("workflowid", "statecode"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("name", ConditionOperator.Equal, flow.Name),
                                new ConditionExpression("category", ConditionOperator.Equal, 5),
                                new ConditionExpression("type", ConditionOperator.Equal, 1)
                            }
                        }
                    };

                    var targetFlow = svc.RetrieveMultiple(query).Entities.FirstOrDefault();
                    if (targetFlow == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Flow not found on {targetDetail.ConnectionName}";
                        results.Add(result);
                        continue;
                    }

                    result.TargetFlowId = targetFlow.Id;

                    var setState = new SetStateRequest
                    {
                        EntityMoniker = targetFlow.ToEntityReference(),
                        State = new OptionSetValue(activate ? 1 : 0),
                        Status = new OptionSetValue(activate ? 2 : 1)
                    };

                    svc.Execute(setState);
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    result.IsConnectionRefError = ex.Message.Contains("ConnectionAuthorizationFailed")
                        || ex.Message.Contains("cannot be used to activate");
                }

                results.Add(result);
            }

            return results;
        }
    }
}
