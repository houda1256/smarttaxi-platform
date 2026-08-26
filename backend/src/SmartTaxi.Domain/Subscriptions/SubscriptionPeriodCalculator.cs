using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.Domain.Subscriptions;

/// <summary>Centralizes "how long is one billing cycle" so renewal/subscribe never duplicate this business rule.</summary>
public static class SubscriptionPeriodCalculator
{
    public static DateTime AddBillingPeriod(DateTime from, BillingPeriod billingPeriod) => billingPeriod switch
    {
        BillingPeriod.Monthly => from.AddMonths(1),
        BillingPeriod.Annual => from.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(billingPeriod))
    };
}
