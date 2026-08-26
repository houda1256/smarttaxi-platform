using SmartTaxi.Domain.Subscriptions;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.Domain.Tests.Subscriptions;

public class SubscriptionPeriodCalculatorTests
{
    [Fact]
    public void AddBillingPeriod_Monthly_AddsOneMonth()
    {
        var from = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc), SubscriptionPeriodCalculator.AddBillingPeriod(from, BillingPeriod.Monthly));
    }

    [Fact]
    public void AddBillingPeriod_Annual_AddsOneYear()
    {
        var from = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc), SubscriptionPeriodCalculator.AddBillingPeriod(from, BillingPeriod.Annual));
    }
}
