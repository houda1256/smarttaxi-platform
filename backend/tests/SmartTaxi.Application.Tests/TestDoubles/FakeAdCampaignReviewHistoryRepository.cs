using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAdCampaignReviewHistoryRepository : IAdCampaignReviewHistoryRepository
{
    private readonly List<AdCampaignReviewHistoryEntry> _entries = [];

    public Task AddAsync(AdCampaignReviewHistoryEntry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AdCampaignReviewHistoryEntry>> GetForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<AdCampaignReviewHistoryEntry>>(_entries.Where(e => e.CampaignId == campaignId).ToList());

    public IReadOnlyCollection<AdCampaignReviewHistoryEntry> All => _entries;
}
