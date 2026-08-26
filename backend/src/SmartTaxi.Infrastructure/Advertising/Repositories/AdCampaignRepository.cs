using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Infrastructure.Advertising;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Advertising.Repositories;

internal sealed class AdCampaignRepository : IAdCampaignRepository
{
    private readonly ApplicationDbContext _context;

    public AdCampaignRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AdCampaign?> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken) =>
        _context.AdCampaigns.FirstOrDefaultAsync(campaign => campaign.Id == campaignId, cancellationToken);

    public async Task<PagedResult<AdCampaign>> GetForAdvertiserAsync(Guid advertiserUserId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.AdCampaigns.Where(campaign => campaign.AdvertiserUserId == advertiserUserId);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(campaign => campaign.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdCampaign>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<AdCampaign>> GetPendingReviewAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.AdCampaigns.Where(campaign => campaign.Status == AdCampaignStatus.Submitted || campaign.Status == AdCampaignStatus.UnderReview);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(campaign => campaign.SubmittedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdCampaign>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyCollection<AdCampaign>> GetApprovedAwaitingScheduleAsync(CancellationToken cancellationToken) =>
        await _context.AdCampaigns.Where(campaign => campaign.Status == AdCampaignStatus.Approved).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<AdCampaign>> GetDueForActivationAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        await _context.AdCampaigns
            .Where(campaign => campaign.Status == AdCampaignStatus.Scheduled && campaign.StartAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<AdCampaign>> GetDueForCompletionAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        await _context.AdCampaigns
            .Where(campaign => campaign.Status == AdCampaignStatus.Active && campaign.EndAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AdCampaign campaign, CancellationToken cancellationToken)
    {
        await _context.AdCampaigns.AddAsync(campaign, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AdCampaign campaign, CancellationToken cancellationToken)
    {
        _context.AdCampaigns.Update(campaign);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryTransitionAsync(
        Guid campaignId, IReadOnlyCollection<AdCampaignStatus> allowedFromStatuses, AdCampaignStatus newStatus, bool touchReviewMetadata,
        Guid? reviewedByUserId, string? reviewReason, DateTime utcNow, CancellationToken cancellationToken)
    {
        var isSubmission = newStatus == AdCampaignStatus.Submitted;
        var newReviewedAtUtc = reviewedByUserId != null ? utcNow : (DateTime?)null;

        var rows = await _context.AdCampaigns
            .Where(campaign => campaign.Id == campaignId && allowedFromStatuses.Contains(campaign.Status))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(campaign => campaign.Status, newStatus)
                .SetProperty(campaign => campaign.UpdatedAtUtc, utcNow)
                .SetProperty(campaign => campaign.SubmittedAtUtc, campaign => isSubmission ? utcNow : campaign.SubmittedAtUtc)
                .SetProperty(campaign => campaign.ReviewedByUserId, campaign => touchReviewMetadata ? reviewedByUserId : campaign.ReviewedByUserId)
                .SetProperty(campaign => campaign.ReviewedAtUtc, campaign => touchReviewMetadata ? newReviewedAtUtc : campaign.ReviewedAtUtc)
                .SetProperty(campaign => campaign.ReviewReason, campaign => touchReviewMetadata ? reviewReason : campaign.ReviewReason), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryConsumeBudgetAsync(Guid campaignId, decimal amount, DateTime utcNow, CancellationToken cancellationToken) =>
        await AdvertisingBudgetConsumer.TryConsumeAsync(_context, campaignId, amount, utcNow, cancellationToken) == BudgetConsumptionOutcome.Consumed;

    public async Task<bool> TryMarkSettledAsync(Guid campaignId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.AdCampaigns
            .Where(campaign => campaign.Id == campaignId && campaign.SettledAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(campaign => campaign.SettledAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }
}
