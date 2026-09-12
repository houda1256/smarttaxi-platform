using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Advertising.Repositories;

internal sealed class CampaignCreativeRepository : ICampaignCreativeRepository
{
    private readonly ApplicationDbContext _context;

    public CampaignCreativeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<CampaignCreative?> GetByIdAsync(Guid creativeId, CancellationToken cancellationToken) =>
        _context.CampaignCreatives.FirstOrDefaultAsync(creative => creative.Id == creativeId, cancellationToken);

    public async Task<IReadOnlyCollection<CampaignCreative>> GetForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        await _context.CampaignCreatives
            .Where(creative => creative.CampaignId == campaignId)
            .OrderByDescending(creative => creative.Version)
            .ToListAsync(cancellationToken);

    public Task<CampaignCreative?> GetCurrentForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        _context.CampaignCreatives
            .Where(creative => creative.CampaignId == campaignId && creative.Status != AdMediaStatus.Replaced)
            .OrderByDescending(creative => creative.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(CampaignCreative creative, CancellationToken cancellationToken)
    {
        await _context.CampaignCreatives.AddAsync(creative, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceAsync(Guid previousCreativeId, CampaignCreative newVersion, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.CampaignCreatives
            .Where(creative => creative.Id == previousCreativeId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(creative => creative.Status, AdMediaStatus.Replaced), cancellationToken);

        await _context.CampaignCreatives.AddAsync(newVersion, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> TryReviewAsync(
        Guid creativeId, bool approved, Guid reviewerUserId, string? rejectionReason, DateTime utcNow, CancellationToken cancellationToken)
    {
        var newStatus = approved ? AdMediaStatus.Approved : AdMediaStatus.Rejected;

        var rows = await _context.CampaignCreatives
            .Where(creative => creative.Id == creativeId && creative.Status == AdMediaStatus.PendingReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(creative => creative.Status, newStatus)
                .SetProperty(creative => creative.ReviewedByUserId, reviewerUserId)
                .SetProperty(creative => creative.ReviewedAtUtc, utcNow)
                .SetProperty(creative => creative.RejectionReason, rejectionReason), cancellationToken);

        return rows == 1;
    }
}
