using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetCampaignDetails;

public sealed record CampaignDetails(
    AdCampaign Campaign, IReadOnlyCollection<CampaignCreative> Creatives, IReadOnlyCollection<AdCampaignReviewHistoryEntry> ReviewHistory);
