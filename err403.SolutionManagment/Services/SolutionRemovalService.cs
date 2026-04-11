using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace err403.SolutionManagment.Services
{
    /// <summary>
    /// Removes solutions from target and source environments.
    /// Pure execution — no UI. Returns results for React to display.
    /// </summary>
    public static class SolutionRemovalService
    {
        public class RemovalResult
        {
            public string SolutionName { get; set; }
            public string TargetName { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public bool IsManaged { get; set; }
            public bool Skipped { get; set; }
        }

        public static RemovalResult RemoveFromTarget(
            string uniqueName,
            string friendlyName,
            ConnectionDetail targetDetail)
        {
            var result = new RemovalResult
            {
                SolutionName = friendlyName,
                TargetName = targetDetail.ConnectionName
            };

            try
            {
                var svc = targetDetail.GetCrmServiceClient();
                var query = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("solutionid", "ismanaged", "friendlyname"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("uniquename", ConditionOperator.Equal, uniqueName)
                        }
                    }
                };

                var targetSolution = svc.RetrieveMultiple(query).Entities.FirstOrDefault();
                if (targetSolution == null)
                {
                    result.Skipped = true;
                    result.ErrorMessage = "Solution not found on target";
                    return result;
                }

                result.IsManaged = targetSolution.GetAttributeValue<bool>("ismanaged");
                svc.Delete("solution", targetSolution.Id);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public static RemovalResult RemoveFromSource(
            string uniqueName,
            string friendlyName,
            Guid solutionId,
            IOrganizationService sourceService)
        {
            var result = new RemovalResult
            {
                SolutionName = friendlyName,
                TargetName = "Source"
            };

            try
            {
                sourceService.Delete("solution", solutionId);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }
}
