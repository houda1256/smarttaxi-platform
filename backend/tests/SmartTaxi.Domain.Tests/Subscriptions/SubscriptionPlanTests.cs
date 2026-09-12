using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.Domain.Tests.Subscriptions;

public class SubscriptionPlanTests
{
    private static SubscriptionPlan NewPlan(decimal price = 29.90m, int trialDays = 0) => SubscriptionPlan.Create(
        "Driver Pro", "DRIVER_PRO", "Plan pour chauffeurs professionnels", UserRole.Driver, price, "TND",
        BillingPeriod.Monthly, trialDays, ["PriorityDispatch"], new Dictionary<string, int> { ["VehiclesCount"] = 3 },
        DateTime.UtcNow);

    [Fact]
    public void Create_WithValidData_SetsExpectedDefaults()
    {
        var plan = NewPlan();

        Assert.Equal("DRIVER_PRO", plan.Code);
        Assert.True(plan.IsActive);
        Assert.Equal(UserRole.Driver, plan.TargetRole);
        Assert.Contains("PriorityDispatch", plan.Features);
        Assert.Equal(3, plan.Limits["VehiclesCount"]);
    }

    [Fact]
    public void Create_NormalizesCodeToUpperInvariant()
    {
        var plan = SubscriptionPlan.Create(
            "Driver Pro", "driver_pro", "desc", UserRole.Driver, 10m, "tnd", BillingPeriod.Monthly, 0, [], new Dictionary<string, int>(),
            DateTime.UtcNow);

        Assert.Equal("DRIVER_PRO", plan.Code);
        Assert.Equal("TND", plan.Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => SubscriptionPlan.Create(
            name, "CODE", "desc", UserRole.Driver, 10m, "TND", BillingPeriod.Monthly, 0, [], new Dictionary<string, int>(), DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        Assert.Throws<ArgumentException>(() => SubscriptionPlan.Create(
            "Name", "CODE", "desc", UserRole.Driver, -1m, "TND", BillingPeriod.Monthly, 0, [], new Dictionary<string, int>(), DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithInvalidCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => SubscriptionPlan.Create(
            "Name", "CODE", "desc", UserRole.Driver, 10m, "TN", BillingPeriod.Monthly, 0, [], new Dictionary<string, int>(), DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithNegativeTrialPeriod_Throws()
    {
        Assert.Throws<ArgumentException>(() => SubscriptionPlan.Create(
            "Name", "CODE", "desc", UserRole.Driver, 10m, "TND", BillingPeriod.Monthly, -1, [], new Dictionary<string, int>(), DateTime.UtcNow));
    }

    [Fact]
    public void UpdateDetails_ChangesNameDescriptionPriceFeaturesLimits()
    {
        var plan = NewPlan();
        var utcNow = DateTime.UtcNow;

        plan.UpdateDetails("Driver Elite", "Nouvelle description", 39.90m, ["PriorityDispatch", "AdvancedAnalytics"],
            new Dictionary<string, int> { ["VehiclesCount"] = 5 }, utcNow);

        Assert.Equal("Driver Elite", plan.Name);
        Assert.Equal(39.90m, plan.Price);
        Assert.Contains("AdvancedAnalytics", plan.Features);
        Assert.Equal(5, plan.Limits["VehiclesCount"]);
        Assert.Equal(utcNow, plan.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_WithNegativePrice_Throws()
    {
        var plan = NewPlan();

        Assert.Throws<ArgumentException>(() => plan.UpdateDetails(
            "Name", "desc", -5m, [], new Dictionary<string, int>(), DateTime.UtcNow));
    }
}
