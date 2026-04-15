using Xunit;
using err403.SolutionManagment.Services;

namespace err403.SolutionManagment.Tests
{
    public class BumpVersionTests
    {
        // ── Fixed-increment policies ──────────────────────────────────────

        [Theory]
        [InlineData("1.2.3.4", "Major", "2.0.0.0")]
        [InlineData("0.0.0.0", "Major", "1.0.0.0")]
        [InlineData("5.9.99.999", "Major", "6.0.0.0")]
        public void Major_IncreasesMajorAndZerosRest(string version, string policy, string expected)
        {
            Assert.Equal(expected, SolutionTransferService.BumpVersion(version, policy));
        }

        [Theory]
        [InlineData("1.2.3.4", "Minor", "1.3.0.0")]
        [InlineData("3.0.0.0", "Minor", "3.1.0.0")]
        [InlineData("0.9.5.2", "Minor", "0.10.0.0")]
        public void Minor_IncreasesMinorAndZerosBuildRevision(string version, string policy, string expected)
        {
            Assert.Equal(expected, SolutionTransferService.BumpVersion(version, policy));
        }

        [Theory]
        [InlineData("1.2.3.4", "Build", "1.2.4.0")]
        [InlineData("2.5.0.0", "Build", "2.5.1.0")]
        public void Build_IncreasesBuildAndZerosRevision(string version, string policy, string expected)
        {
            Assert.Equal(expected, SolutionTransferService.BumpVersion(version, policy));
        }

        [Theory]
        [InlineData("1.2.3.4", "Revision", "1.2.3.5")]
        [InlineData("2.0.1.0", "Revision", "2.0.1.1")]
        public void Revision_IncreasesOnlyRevision(string version, string policy, string expected)
        {
            Assert.Equal(expected, SolutionTransferService.BumpVersion(version, policy));
        }

        // ── Unknown / Skip policy ─────────────────────────────────────────

        [Theory]
        [InlineData("1.2.3.4", "Skip")]
        [InlineData("1.2.3.4", "")]
        [InlineData("1.2.3.4", "Unknown")]
        public void UnknownPolicy_ReturnsVersionUnchanged(string version, string policy)
        {
            Assert.Equal(version, SolutionTransferService.BumpVersion(version, policy));
        }

        // ── Short version strings (fewer than 4 parts) ────────────────────

        [Theory]
        [InlineData("3", "Minor", "3.1.0.0")]
        [InlineData("1.5", "Build", "1.5.1.0")]
        [InlineData("1.2.3", "Revision", "1.2.3.1")]
        public void ShortVersionStrings_ArePaddedCorrectly(string version, string policy, string expected)
        {
            Assert.Equal(expected, SolutionTransferService.BumpVersion(version, policy));
        }

        // ── Date policy ───────────────────────────────────────────────────

        [Fact]
        public void Date_ResultStartsWithCurrentYear()
        {
            var result = SolutionTransferService.BumpVersion("1.0.0.0", "Date", "yyyy.MM.dd.x");
            var yearPrefix = DateTime.Now.Year.ToString();
            Assert.StartsWith(yearPrefix, result);
        }

        [Fact]
        public void Date_CounterIncrementsWhenSameDatePrefix()
        {
            // Manually synthesise a "today" prefix so we don't depend on wall clock
            var today = DateTime.Now;
            var prefix = $"{today.Year}.{today.Month:D2}.{today.Day:D2}.";
            var existingVersion = prefix + "5";

            var result = SolutionTransferService.BumpVersion(existingVersion, "Date", "yyyy.MM.dd.x");
            Assert.Equal(prefix + "6", result);
        }

        [Fact]
        public void Date_CounterResetsTo1WhenDateHasChanged()
        {
            // A date from yesterday will not match today's prefix → reset to 1
            var yesterday = DateTime.Now.AddDays(-1);
            var oldVersion = $"{yesterday.Year}.{yesterday.Month:D2}.{yesterday.Day:D2}.3";

            var result = SolutionTransferService.BumpVersion(oldVersion, "Date", "yyyy.MM.dd.x");

            var todayPrefix = $"{DateTime.Now.Year}.{DateTime.Now.Month:D2}.{DateTime.Now.Day:D2}.";
            Assert.StartsWith(todayPrefix, result);
            Assert.EndsWith(".1", result);
        }
    }
}
