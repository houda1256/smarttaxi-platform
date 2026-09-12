namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>The spec's "Global" analytics bucket (line 329) — all 9 named growth metrics, computed for the same [FromUtc, ToUtc) period in one call.</summary>
public sealed record GrowthAnalyticsResult(
    DateTime FromUtc,
    DateTime ToUtc,
    GrowthMetric UserGrowth,
    GrowthMetric RideGrowth,
    GrowthMetric RevenueGrowth,
    GrowthMetric SubscriptionGrowth,
    GrowthMetric PartnerGrowth,
    GrowthMetric VehicleGrowth,
    GrowthMetric CampaignGrowth,
    GrowthMetric SupportTicketGrowth,
    GrowthMetric IncidentGrowth,
    string? UserGrowthNote);
