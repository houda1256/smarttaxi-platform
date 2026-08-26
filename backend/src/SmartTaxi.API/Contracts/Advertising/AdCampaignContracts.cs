using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.API.Contracts.Advertising;

public sealed record CreateCampaignRequest(
    string Name, string Description, string Objective, Guid PlacementId, DateTime StartAtUtc, DateTime EndAtUtc, string PricingModel,
    decimal PriceRate, decimal BudgetLimit, decimal? DailyBudgetLimit, string? TargetCity, string? TargetVehicleCategory,
    string? TargetDaysOfWeek, int? TargetStartHour, int? TargetEndHour);

public sealed record UpdateCampaignRequest(
    string Name, string Description, string Objective, Guid PlacementId, DateTime StartAtUtc, DateTime EndAtUtc, string PricingModel,
    decimal PriceRate, decimal BudgetLimit, decimal? DailyBudgetLimit, string? TargetCity, string? TargetVehicleCategory,
    string? TargetDaysOfWeek, int? TargetStartHour, int? TargetEndHour);

public sealed record RejectCampaignRequest(string Reason);

public sealed record RequestCampaignChangesRequest(string Reason);

public sealed record SuspendCampaignRequest(string Reason);

public sealed record AdCampaignResponse(
    Guid Id, string Code, string Name, string Description, string Objective, Guid PlacementId, DateTime StartAtUtc, DateTime EndAtUtc,
    string Status, string PricingModel, decimal PriceRate, decimal BudgetLimit, decimal ConsumedBudget, decimal RemainingBudget,
    decimal? DailyBudgetLimit, string? TargetCity, string? TargetVehicleCategory, string? TargetDaysOfWeek, int? TargetStartHour,
    int? TargetEndHour, DateTime CreatedAtUtc, DateTime? SubmittedAtUtc, DateTime? ReviewedAtUtc, Guid? ReviewedByUserId,
    string? ReviewReason)
{
    public static AdCampaignResponse FromEntity(AdCampaign campaign) =>
        new(campaign.Id, campaign.Code, campaign.Name, campaign.Description, campaign.Objective, campaign.PlacementId, campaign.StartAtUtc,
            campaign.EndAtUtc, campaign.Status.ToString(), campaign.PricingModel.ToString(), campaign.PriceRate, campaign.BudgetLimit,
            campaign.ConsumedBudget, campaign.RemainingBudget, campaign.DailyBudgetLimit, campaign.TargetCity, campaign.TargetVehicleCategory,
            campaign.TargetDaysOfWeek, campaign.TargetStartHour, campaign.TargetEndHour, campaign.CreatedAtUtc, campaign.SubmittedAtUtc,
            campaign.ReviewedAtUtc, campaign.ReviewedByUserId, campaign.ReviewReason);
}

public sealed record CampaignReviewHistoryEntryResponse(Guid Id, Guid? ReviewerUserId, string Action, string PreviousStatus, string NewStatus, string? Reason, DateTime CreatedAtUtc)
{
    public static CampaignReviewHistoryEntryResponse FromEntity(AdCampaignReviewHistoryEntry entry) =>
        new(entry.Id, entry.ReviewerUserId, entry.Action.ToString(), entry.PreviousStatus.ToString(), entry.NewStatus.ToString(), entry.Reason, entry.CreatedAtUtc);
}

public sealed record CampaignCreativeResponse(
    Guid Id, string MediaType, string MimeType, string DisplayName, long FileSizeBytes, string Status, int Version, DateTime CreatedAtUtc)
{
    public static CampaignCreativeResponse FromEntity(CampaignCreative creative) =>
        new(creative.Id, creative.MediaType.ToString(), creative.MimeType, creative.DisplayName, creative.FileSizeBytes, creative.Status.ToString(), creative.Version, creative.CreatedAtUtc);
}

public sealed record CampaignDetailsResponse(AdCampaignResponse Campaign, IReadOnlyCollection<CampaignCreativeResponse> Creatives, IReadOnlyCollection<CampaignReviewHistoryEntryResponse> ReviewHistory);

public sealed record CampaignPerformanceResponse(Guid CampaignId, int Impressions, int Clicks, decimal Ctr, decimal BudgetLimit, decimal ConsumedBudget, decimal RemainingBudget);
