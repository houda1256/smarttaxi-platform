using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Abstractions;

public interface ICampaignCreativeRepository
{
    Task<CampaignCreative?> GetByIdAsync(Guid creativeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CampaignCreative>> GetForCampaignAsync(Guid campaignId, CancellationToken cancellationToken);

    Task<CampaignCreative?> GetCurrentForCampaignAsync(Guid campaignId, CancellationToken cancellationToken);

    Task AddAsync(CampaignCreative creative, CancellationToken cancellationToken);

    /// <summary>Atomically marks the previous version Replaced and inserts the new version in one transaction — a creative is never edited in place.</summary>
    Task ReplaceAsync(Guid previousCreativeId, CampaignCreative newVersion, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryReviewAsync(Guid creativeId, bool approved, Guid reviewerUserId, string? rejectionReason, DateTime utcNow, CancellationToken cancellationToken);
}
