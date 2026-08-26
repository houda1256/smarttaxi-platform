namespace SmartTaxi.Application.Advertising.Queries.GetCampaignPerformance;

/// <summary>Aggregated, privacy-safe facts only — no passenger/individual identity ever appears here. Ctr is division-by-zero-safe (0 when Impressions is 0).</summary>
public sealed record CampaignPerformanceSummary(
    Guid CampaignId, int Impressions, int Clicks, decimal Ctr, decimal BudgetLimit, decimal ConsumedBudget, decimal RemainingBudget);
