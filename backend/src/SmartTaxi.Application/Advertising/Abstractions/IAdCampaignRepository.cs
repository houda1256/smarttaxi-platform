using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Abstractions;

public interface IAdCampaignRepository
{
    Task<AdCampaign?> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken);

    Task<PagedResult<AdCampaign>> GetForAdvertiserAsync(Guid advertiserUserId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<AdCampaign>> GetPendingReviewAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Approved campaigns not yet marked Scheduled — the first stage of the activation sweep.</summary>
    Task<IReadOnlyCollection<AdCampaign>> GetApprovedAwaitingScheduleAsync(CancellationToken cancellationToken);

    /// <summary>Scheduled campaigns whose StartAtUtc has arrived — the second stage of the activation sweep.</summary>
    Task<IReadOnlyCollection<AdCampaign>> GetDueForActivationAsync(DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Active campaigns whose EndAtUtc has passed.</summary>
    Task<IReadOnlyCollection<AdCampaign>> GetDueForCompletionAsync(DateTime utcNow, CancellationToken cancellationToken);

    Task AddAsync(AdCampaign campaign, CancellationToken cancellationToken);

    /// <summary>Persists non-status field edits (Draft edits, or a material edit that the caller also transitions back to Submitted in the same handler) — never used to move Status by itself.</summary>
    Task UpdateAsync(AdCampaign campaign, CancellationToken cancellationToken);

    /// <summary>
    /// Atomic conditional transition: succeeds only if the campaign's current Status is one of
    /// allowedFromStatuses, and applies the new status in the same UPDATE statement — the single
    /// source of truth for every lifecycle move, race-safe under concurrent callers (e.g. approve vs.
    /// reject). touchReviewMetadata controls whether ReviewedByUserId/ReviewedAtUtc/ReviewReason are
    /// overwritten (true for actual review-type actions — Submit/Approve/Reject/RequestChanges/
    /// Suspend/Reactivate/material-change-resubmission) or left untouched (false for plain lifecycle
    /// moves — Pause/Resume/Cancel/the Scheduled/Active/Completed sweep — which must never erase who
    /// approved the campaign).
    /// </summary>
    Task<bool> TryTransitionAsync(
        Guid campaignId, IReadOnlyCollection<AdCampaignStatus> allowedFromStatuses, AdCampaignStatus newStatus,
        bool touchReviewMetadata, Guid? reviewedByUserId, string? reviewReason, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Atomic conditional budget consumption: succeeds only if the campaign is Active and both the
    /// total remaining budget and (when configured) the remaining daily budget can absorb the amount —
    /// never a load-check-save sequence. The daily counter resets automatically when the stored
    /// DailyConsumedDateUtc no longer matches today's UTC date.
    /// </summary>
    Task<bool> TryConsumeBudgetAsync(Guid campaignId, decimal amount, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Atomic conditional flag flip guarding against double-settlement — see IAdvertisingBillingService.</summary>
    Task<bool> TryMarkSettledAsync(Guid campaignId, DateTime utcNow, CancellationToken cancellationToken);
}
