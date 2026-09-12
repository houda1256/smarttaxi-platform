using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Abstractions;

public interface IAdCampaignReviewHistoryRepository
{
    Task AddAsync(AdCampaignReviewHistoryEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdCampaignReviewHistoryEntry>> GetForCampaignAsync(Guid campaignId, CancellationToken cancellationToken);
}
