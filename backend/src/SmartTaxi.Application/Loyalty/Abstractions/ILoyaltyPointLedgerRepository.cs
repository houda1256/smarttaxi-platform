using SmartTaxi.Application.Common;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Loyalty.Abstractions;

public interface ILoyaltyPointLedgerRepository
{
    Task<LoyaltyPointLedgerEntry?> GetByIdAsync(Guid entryId, CancellationToken cancellationToken);

    Task<LoyaltyPointLedgerEntry?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<PagedResult<LoyaltyPointLedgerEntry>> GetForUserAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Earn entries whose ExpirationAtUtc is due and that have not yet produced a corresponding Expire entry (see ProcessExpiredPointsCommand).</summary>
    Task<IReadOnlyCollection<LoyaltyPointLedgerEntry>> GetDueForExpirationAsync(DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Used to derive challenge progress from Loyalty's own data (e.g. RideCount = count of Earn entries sourced from a Payment) — never reads Rides/Identity tables directly.</summary>
    Task<int> CountEntriesAsync(Guid userId, string sourceType, LoyaltyLedgerEntryType entryType, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically increments the account's balance (unconditional — a credit
    /// can never be refused for insufficient funds) and appends the
    /// idempotency-guarded ledger row in one transaction. Returns null
    /// (no-op, not a failure) if this exact (SourceType, SourceId, PointType,
    /// EntryType) was already credited.
    /// </summary>
    Task<LoyaltyPointLedgerEntry?> TryCreditAsync(
        LoyaltyLedgerAppendRequest request, IReadOnlyCollection<LoyaltyTierThreshold> statusTierThresholds, DateTime utcNow,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically decrements the account's balance only if sufficient
    /// (conditional UPDATE guarded on CurrentRewardPoints >= request.Points —
    /// the mechanism that makes "two concurrent spends against the same final
    /// balance" safe) and appends the idempotency-guarded ledger row. Returns
    /// null if either the balance was insufficient or this exact source was
    /// already debited.
    /// </summary>
    Task<LoyaltyPointLedgerEntry?> TryDebitAsync(LoyaltyLedgerAppendRequest request, DateTime utcNow, CancellationToken cancellationToken);
}
