using SmartTaxi.Domain.Analytics.Entities;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Domain.Tests.Analytics.Entities;

public class ScheduledReportDefinitionTests
{
    [Fact]
    public void Create_ValidRequest_Succeeds()
    {
        var utcNow = DateTime.UtcNow;
        var firstRun = utcNow.AddDays(1);

        var definition = ScheduledReportDefinition.Create(
            ScheduledReportCategory.Ride, ScheduledReportFrequency.Daily, Guid.NewGuid(), firstRun, utcNow);

        Assert.NotEqual(Guid.Empty, definition.Id);
        Assert.True(definition.IsActive);
        Assert.Equal(firstRun, definition.NextRunAtUtc);
        Assert.Null(definition.LastProcessedAtUtc);
        Assert.Null(definition.ProcessingClaimedAtUtc);
    }

    [Fact]
    public void Create_EmptyRecipient_Throws()
    {
        Assert.Throws<ArgumentException>(() => ScheduledReportDefinition.Create(
            ScheduledReportCategory.Financial, ScheduledReportFrequency.Weekly, Guid.Empty, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(ScheduledReportFrequency.Daily, 1)]
    [InlineData(ScheduledReportFrequency.Weekly, 7)]
    public void ComputeNextRun_DailyAndWeekly_AddsExactDays(ScheduledReportFrequency frequency, int expectedDays)
    {
        var previousRun = new DateTime(2026, 3, 1, 6, 0, 0, DateTimeKind.Utc);

        var nextRun = ScheduledReportDefinition.ComputeNextRun(frequency, previousRun);

        Assert.Equal(previousRun.AddDays(expectedDays), nextRun);
    }

    [Theory]
    [InlineData(ScheduledReportFrequency.Monthly, 1)]
    [InlineData(ScheduledReportFrequency.Quarterly, 3)]
    public void ComputeNextRun_MonthlyAndQuarterly_AddsExactCalendarMonths(ScheduledReportFrequency frequency, int expectedMonths)
    {
        var previousRun = new DateTime(2026, 1, 31, 6, 0, 0, DateTimeKind.Utc);

        var nextRun = ScheduledReportDefinition.ComputeNextRun(frequency, previousRun);

        Assert.Equal(previousRun.AddMonths(expectedMonths), nextRun);
    }

    [Fact]
    public void ComputeCoveredPeriod_Daily_CoversPrecedingTwentyFourHours()
    {
        var occurrence = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);

        var (fromUtc, toUtc) = ScheduledReportDefinition.ComputeCoveredPeriod(ScheduledReportFrequency.Daily, occurrence);

        Assert.Equal(occurrence.AddDays(-1), fromUtc);
        Assert.Equal(occurrence, toUtc);
    }

    [Fact]
    public void ComputeCoveredPeriod_Quarterly_CoversPrecedingThreeCalendarMonths()
    {
        var occurrence = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var (fromUtc, toUtc) = ScheduledReportDefinition.ComputeCoveredPeriod(ScheduledReportFrequency.Quarterly, occurrence);

        Assert.Equal(occurrence.AddMonths(-3), fromUtc);
        Assert.Equal(occurrence, toUtc);
    }
}
