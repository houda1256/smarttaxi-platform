namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>ActiveSubscriptions uses Subscription.IsEffectivelyActive(utcNow) — never Status==Active alone, matching the entity's own documented rule.</summary>
public sealed record SubscriptionAnalyticsSummary(DateTime FromUtc, DateTime ToUtc, int ActiveSubscriptions, int SubscriptionGrowthCount);
