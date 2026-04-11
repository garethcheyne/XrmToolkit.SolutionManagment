using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace err403.SolutionManagment.Services
{
    /// <summary>
    /// Retrieves missing component dependencies after import.
    /// Pure execution — no UI. Returns JSON for React to display.
    /// </summary>
    public static class MissingDependencyService
    {
        public class MissingComponent
        {
            [JsonProperty("requiredType")] public string RequiredType { get; set; }
            [JsonProperty("requiredName")] public string RequiredName { get; set; }
            [JsonProperty("requiredSchemaName")] public string RequiredSchemaName { get; set; }
            [JsonProperty("requiredSolution")] public string RequiredSolution { get; set; }
            [JsonProperty("dependentType")] public string DependentType { get; set; }
            [JsonProperty("dependentName")] public string DependentName { get; set; }
        }

        public static List<MissingComponent> GetMissingComponents(
            IOrganizationService targetService,
            IOrganizationService sourceService,
            Guid importJobId)
        {
            var results = new List<MissingComponent>();

            try
            {
                // Retrieve import job data
                var importJob = targetService.Retrieve("importjob", importJobId,
                    new Microsoft.Xrm.Sdk.Query.ColumnSet("data"));

                var data = importJob.GetAttributeValue<string>("data");
                if (string.IsNullOrEmpty(data)) return results;

                var doc = XDocument.Parse(data);
                var missingDeps = doc.Descendants("MissingDependency");

                foreach (var dep in missingDeps)
                {
                    var required = dep.Element("Required");
                    var dependent = dep.Element("Dependent");

                    if (required != null)
                    {
                        results.Add(new MissingComponent
                        {
                            RequiredType = required.Attribute("type")?.Value ?? "",
                            RequiredName = required.Attribute("displayName")?.Value ?? required.Attribute("schemaName")?.Value ?? "",
                            RequiredSchemaName = required.Attribute("schemaName")?.Value ?? "",
                            RequiredSolution = required.Attribute("solution")?.Value ?? "",
                            DependentType = dependent?.Attribute("type")?.Value ?? "",
                            DependentName = dependent?.Attribute("displayName")?.Value ?? dependent?.Attribute("schemaName")?.Value ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[MissingDependencyService] Error: {ex.Message}");
            }

            return results;
        }
    }
}
