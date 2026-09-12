namespace SmartTaxi.Application.Tests.Analytics;

public class AnalyticsDateRangeTests
{
    [Fact]
    public void IsValid_FromBeforeToWithinCap_ReturnsTrue()
    {
        var fromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(30);

        Assert.True(SmartTaxi.Application.Analytics.AnalyticsDateRange.IsValid(fromUtc, toUtc));
    }

    [Fact]
    public void IsValid_FromEqualsTo_ReturnsFalse()
    {
        var instant = DateTime.UtcNow;

        Assert.False(SmartTaxi.Application.Analytics.AnalyticsDateRange.IsValid(instant, instant));
    }

    [Fact]
    public void IsValid_FromAfterTo_ReturnsFalse()
    {
        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddDays(-1);

        Assert.False(SmartTaxi.Application.Analytics.AnalyticsDateRange.IsValid(fromUtc, toUtc));
    }

    [Fact]
    public void IsValid_ExceedsMaxRangeDays_ReturnsFalse()
    {
        var fromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(367);

        Assert.False(SmartTaxi.Application.Analytics.AnalyticsDateRange.IsValid(fromUtc, toUtc));
    }

    [Fact]
    public void IsValid_ExactlyAtMaxRangeDays_ReturnsTrue()
    {
        var fromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(366);

        Assert.True(SmartTaxi.Application.Analytics.AnalyticsDateRange.IsValid(fromUtc, toUtc));
    }
}
