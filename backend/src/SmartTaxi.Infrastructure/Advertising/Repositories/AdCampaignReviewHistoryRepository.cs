using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Advertising.Repositories;

internal sealed class AdCampaignReviewHistoryRepository : IAdCampaignReviewHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public AdCampaignReviewHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdCampaignReviewHistoryEntry entry, CancellationToken cancellationToken)
    {
        await _context.AdCampaignReviewHistoryEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AdCampaignReviewHistoryEntry>> GetForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        await _context.AdCampaignReviewHistoryEntries
            .Where(entry => entry.CampaignId == campaignId)
            .OrderBy(entry => entry.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
