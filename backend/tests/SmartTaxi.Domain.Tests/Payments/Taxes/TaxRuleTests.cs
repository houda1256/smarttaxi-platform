using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.Domain.Tests.Payments.Taxes;

public class TaxRuleTests
{
    [Fact]
    public void IsApplicableOn_WithinEffectiveWindowAndMatchingService_ReturnsTrue()
    {
        var rule = TaxRule.Create("TVA", 19m, "TN", "Ride", new DateOnly(2026, 1, 1), null, null, DateTime.UtcNow);

        Assert.True(rule.IsApplicableOn(new DateOnly(2026, 6, 1), "Ride"));
    }

    [Fact]
    public void IsApplicableOn_BeforeEffectiveFrom_ReturnsFalse()
    {
        var rule = TaxRule.Create("TVA", 19m, "TN", "Ride", new DateOnly(2026, 6, 1), null, null, DateTime.UtcNow);

        Assert.False(rule.IsApplicableOn(new DateOnly(2026, 1, 1), "Ride"));
    }

    [Fact]
    public void IsApplicableOn_AfterEffectiveTo_ReturnsFalse()
    {
        var rule = TaxRule.Create("TVA", 19m, "TN", "Ride", new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1), null, DateTime.UtcNow);

        Assert.False(rule.IsApplicableOn(new DateOnly(2026, 6, 1), "Ride"));
    }

    [Fact]
    public void IsApplicableOn_ForDifferentService_ReturnsFalseUnlessAll()
    {
        var rideOnly = TaxRule.Create("TVA", 19m, "TN", "Ride", new DateOnly(2026, 1, 1), null, null, DateTime.UtcNow);
        var allServices = TaxRule.Create("TVA", 19m, "TN", "All", new DateOnly(2026, 1, 1), null, null, DateTime.UtcNow);

        Assert.False(rideOnly.IsApplicableOn(new DateOnly(2026, 6, 1), "Subscription"));
        Assert.True(allServices.IsApplicableOn(new DateOnly(2026, 6, 1), "Subscription"));
    }

    [Fact]
    public void Create_WithEffectiveToBeforeFrom_Throws()
    {
        Assert.Throws<ArgumentException>(() => TaxRule.Create(
            "TVA", 19m, "TN", "Ride", new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1), null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithNegativeRate_Throws()
    {
        Assert.Throws<ArgumentException>(() => TaxRule.Create("TVA", -1m, "TN", "Ride", new DateOnly(2026, 1, 1), null, null, DateTime.UtcNow));
    }
}
