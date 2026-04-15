using Xunit;
using err403.SolutionManagment.Services;

namespace err403.SolutionManagment.Tests
{
    public class ParseImportErrorTests
    {
        // ─── Helpers ─────────────────────────────────────────────────────────

        /// <summary>Wraps a message in the "Exception type / Message / Detail" format used by Dataverse.</summary>
        private static string Wrap(string msg) =>
            $"Exception type: FaultException\nMessage: {msg}\nDetail:\n  (detail omitted)";

        private static string MissingDepXml(
            string depName, string depType,
            string reqName, string reqType,
            string? solution = "CoreSolution",
            string? reqId = "{11111111-0000-0000-0000-000000000001}",
            string canResolve = "True") =>
            $"<MissingDependencies>" +
            $"<MissingDependency canResolveMissingDependency=\"{canResolve}\">" +
            $"<Required type=\"{reqType}\" displayName=\"{reqName}\"" +
            (reqId != null ? $" id=\"{reqId}\"" : "") +
            (solution != null ? $" solution=\"{solution}\"" : "") +
            $"/>" +
            $"<Dependent type=\"{depType}\" displayName=\"{depName}\" id=\"{{AAAAAAAA-0000-0000-0000-000000000001}}\"/>" +
            $"</MissingDependency></MissingDependencies>";

        // ─── Null / empty input ───────────────────────────────────────────────

        [Fact]
        public void NullOrEmpty_ReturnsImportFailed()
        {
            Assert.Equal("Import failed", SolutionTransferService.ParseImportErrorMessage(null!));
            Assert.Equal("Import failed", SolutionTransferService.ParseImportErrorMessage(""));
            Assert.Equal("Import failed", SolutionTransferService.ParseImportErrorMessage("   "));
        }

        // ─── Plain messages (no XML) ──────────────────────────────────────────

        [Fact]
        public void PlainMessage_IsReturnedTrimmed()
        {
            var raw = Wrap("Solution import failed for an unknown reason.");
            var result = SolutionTransferService.ParseImportErrorMessage(raw);
            Assert.Equal("Solution import failed for an unknown reason.", result);
        }

        [Fact]
        public void PlainMessage_StripsFailurePrefix()
        {
            var raw = Wrap("Solution manifest import: FAILURE: The publisher prefix is reserved.");
            var result = SolutionTransferService.ParseImportErrorMessage(raw);
            Assert.Equal("The publisher prefix is reserved.", result);
        }

        [Fact]
        public void PlainMessage_StripsProductUpdatesOnlySuffix()
        {
            var raw = Wrap("Version mismatch error, ProductUpdatesOnly : False");
            var result = SolutionTransferService.ParseImportErrorMessage(raw);
            Assert.DoesNotContain("ProductUpdatesOnly", result);
        }

        [Fact]
        public void MessageWithoutWrapper_StillReturnsUsefulText()
        {
            // No "Message: ..." tag — raw string is used directly
            var result = SolutionTransferService.ParseImportErrorMessage("Direct error text here.");
            Assert.Equal("Direct error text here.", result);
        }

        // ─── MissingDependency XML ────────────────────────────────────────────

        [Fact]
        public void MissingDeps_FormatsIntroAndDependencyList()
        {
            var xml = MissingDepXml("My Workflow", "29", "Base Process", "29");
            var raw = Wrap($"Solution manifest import: FAILURE: Missing components {xml}");

            var result = SolutionTransferService.ParseImportErrorMessage(raw);

            Assert.Contains("Missing Dependencies:", result);
            Assert.Contains("My Workflow", result);
            Assert.Contains("Base Process", result);
        }

        [Fact]
        public void MissingDeps_MapsComponentTypeCodeToName()
        {
            var xml = MissingDepXml("Some workflow", "29", "Parent workflow", "29");
            var raw = Wrap($"Failure {xml}");

            var result = SolutionTransferService.ParseImportErrorMessage(raw);

            // "29" should map to "Workflow"
            Assert.Contains("Workflow", result);
        }

        [Fact]
        public void MissingDeps_IncludesSolutionAndIdWhenPresent()
        {
            var xml = MissingDepXml("Dep A", "29", "Req B", "29", "MySolution", "{GUID-HERE}", "True");
            var raw = Wrap($"Failure {xml}");

            var result = SolutionTransferService.ParseImportErrorMessage(raw);

            Assert.Contains("MySolution", result);
            Assert.Contains("{GUID-HERE}", result);
        }

        [Fact]
        public void MissingDeps_MarksSelfResolvable()
        {
            var xml = MissingDepXml("Dep", "29", "Req", "29", canResolve: "True");
            var raw = Wrap($"Failure {xml}");

            var result = SolutionTransferService.ParseImportErrorMessage(raw);

            Assert.Contains("can be resolved automatically", result);
        }

        [Fact]
        public void MissingDeps_StripsSolutionManifestFailurePrefix()
        {
            var xml = MissingDepXml("Flow A", "29", "Base", "29");
            var raw = Wrap($"Solution manifest import: FAILURE: Components missing {xml}");

            var result = SolutionTransferService.ParseImportErrorMessage(raw);

            Assert.DoesNotContain("Solution manifest import", result);
            Assert.DoesNotContain("FAILURE:", result);
        }

        // ─── GetComponentTypeName via ParseImportErrorMessage ─────────────────

        [Theory]
        [InlineData("1", "Entity")]
        [InlineData("2", "Attribute")]
        [InlineData("29", "Workflow")]
        [InlineData("61", "Web Resource")]
        [InlineData("80", "Model-driven App")]
        [InlineData("91", "Plugin Assembly")]
        [InlineData("92", "SDK Message Processing Step")]
        [InlineData("999", "999")]   // Unknown type code → pass through
        public void ComponentTypeCode_MapsToExpectedName(string typeCode, string expectedName)
        {
            // Embed the type code in a dependency and verify the output contains the mapped name.
            var xml = MissingDepXml("Dependent", typeCode, "Required", typeCode, null, null, "False");
            var raw = Wrap($"Fail {xml}");

            var result = SolutionTransferService.ParseImportErrorMessage(raw);

            Assert.Contains(expectedName, result);
        }
    }
}
