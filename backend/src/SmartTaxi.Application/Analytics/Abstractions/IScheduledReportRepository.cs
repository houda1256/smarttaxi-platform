using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Analytics.Entities;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Analytics.Abstractions;

/// <summary>
/// TryClaimAsync/TryFinalizeAsync implement the mandatory claim-before-deliver
/// pattern: the winner of the atomic claim is determined BEFORE any report is
/// generated or delivered, never after — see ProcessDueScheduledReportsCommand
/// for the full sequence. A claim that is never finalized (process crash
/// between claim and finalize) becomes reclaimable once StaleClaimThreshold
/// has elapsed, which means delivery is at-least-once, not exactly-once: a
/// crash after a successful DispatchAsync but before TryFinalizeAsync commits
/// will cause that occurrence to be redelivered once reclaimed. This is a
/// documented, deliberate trade-off — see ProcessDueScheduledReportsCommand's
/// own remarks for why a stronger guarantee was not implemented.
/// </summary>
public interface IScheduledReportRepository
{
    /// <summary>The bound used by both TryClaimAsync and GetDueIdsAsync to decide when an outstanding claim should be treated as abandoned.</summary>
    static TimeSpan StaleClaimThreshold => TimeSpan.FromMinutes(15);

    Task<bool> TryAddAsync(ScheduledReportDefinition definition, CancellationToken cancellationToken);

    Task<ScheduledReportDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ScheduledReportDefinition>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Ids that are either idle-and-due, or claimed-but-stale (abandoned by a crashed processor) — both are legitimate candidates for TryClaimAsync.</summary>
    Task<IReadOnlyCollection<Guid>> GetDueIdsAsync(DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Guarded WHERE IsActive AND NextRunAtUtc &lt;= utcNow AND (ProcessingClaimedAtUtc IS NULL OR ProcessingClaimedAtUtc &lt; utcNow - StaleClaimThreshold).
    /// Sets ProcessingClaimedAtUtc = utcNow. Only one concurrent caller ever
    /// wins this for the same due occurrence.
    /// </summary>
    Task<bool> TryClaimAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Guarded WHERE ProcessingClaimedAtUtc == the exact claimedAtUtc this caller itself set via TryClaimAsync — never clears a claim this caller did not win. Clears the claim, advances NextRunAtUtc, sets LastProcessedAtUtc.</summary>
    Task<bool> TryFinalizeAsync(Guid id, DateTime claimedAtUtc, DateTime nextRunAtUtc, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryUpdateAsync(
        Guid id, ScheduledReportCategory category, ScheduledReportFrequency frequency, Guid recipientUserId, DateTime utcNow,
        CancellationToken cancellationToken);

    Task<bool> TryDeactivateAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken);
}
