namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>Growth/active-count only — per-campaign impressions/clicks/CTR remain Advertising's own concern (GetCampaignPerformanceAdmin), never duplicated here.</summary>
public sealed record AdvertisingAnalyticsSummary(DateTime FromUtc, DateTime ToUtc, int ActiveCampaigns, int CampaignGrowthCount);
