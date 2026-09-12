using SmartTaxi.Application.Common;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of the real repository's atomic credit/debit-plus-ledger-append transaction — sufficient for Application-layer unit tests; the actual concurrency guarantee is proven against real PostgreSQL in the integration tests.</summary>
public sealed class FakeLoyaltyPointLedgerRepository : ILoyaltyPointLedgerRepository
{
    private readonly Dictionary<Guid, LoyaltyPointLedgerEntry> _entries = new();
    private readonly FakeLoyaltyAccountRepository _accountRepository;

    public FakeLoyaltyPointLedgerRepository(FakeLoyaltyAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public Task<LoyaltyPointLedgerEntry?> GetByIdAsync(Guid entryId, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.GetValueOrDefault(entryId));

    public Task<LoyaltyPointLedgerEntry?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.Values.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey));

    public Task<PagedResult<LoyaltyPointLedgerEntry>> GetForUserAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var ordered = _entries.Values.Where(e => e.UserId == userId).OrderByDescending(e => e.CreatedAtUtc).ToList();
        var page = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<LoyaltyPointLedgerEntry>(page, ordered.Count, pageNumber, pageSize));
    }

    public Task<IReadOnlyCollection<LoyaltyPointLedgerEntry>> GetDueForExpirationAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var candidates = _entries.Values
            .Where(e => e.PointType == LoyaltyPointType.RewardPoints && e.EntryType == LoyaltyLedgerEntryType.Earn
                && e.ExpirationAtUtc != null && e.ExpirationAtUtc <= utcNow)
            .Where(e => !_entries.Values.Any(other =>
                other.IdempotencyKey == LoyaltyPointLedgerEntry.ComputeIdempotencyKey(
                    "LoyaltyPointLedgerEntry", e.Id, e.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire)))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<LoyaltyPointLedgerEntry>>(candidates);
    }

    public Task<int> CountEntriesAsync(Guid userId, string sourceType, LoyaltyLedgerEntryType entryType, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.Values.Count(e => e.UserId == userId && e.SourceType == sourceType && e.EntryType == entryType));

    public Task<LoyaltyPointLedgerEntry?> TryCreditAsync(
        LoyaltyLedgerAppendRequest request, IReadOnlyCollection<LoyaltyTierThreshold> statusTierThresholds, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = LoyaltyPointLedgerEntry.ComputeIdempotencyKey(request.SourceType, request.SourceId, request.UserId, request.PointType, request.EntryType);

        if (_entries.Values.Any(e => e.IdempotencyKey == idempotencyKey))
        {
            return Task.FromResult<LoyaltyPointLedgerEntry?>(null);
        }

        var account = _accountRepository.GetByIdAsync(request.AccountId, cancellationToken).Result;

        if (account is null)
        {
            return Task.FromResult<LoyaltyPointLedgerEntry?>(null);
        }

        int balanceAfter;

        if (request.PointType == LoyaltyPointType.RewardPoints)
        {
            account.ApplyRewardPointsDelta(request.Points, utcNow);
            balanceAfter = account.CurrentRewardPoints;
        }
        else
        {
            account.ApplyStatusPointsDelta(request.Points, statusTierThresholds, utcNow);
            balanceAfter = account.CurrentStatusPoints;
        }

        var entry = LoyaltyPointLedgerEntry.Create(
            request.AccountId, request.UserId, request.PointType, request.EntryType, request.Points, balanceAfter, request.SourceType,
            request.SourceId, request.Reason, utcNow, request.EarningRuleId, request.RewardId, request.ReferralId, request.ExpirationAtUtc,
            request.CreatedBy);

        _entries[entry.Id] = entry;
        return Task.FromResult<LoyaltyPointLedgerEntry?>(entry);
    }

    public Task<LoyaltyPointLedgerEntry?> TryDebitAsync(LoyaltyLedgerAppendRequest request, DateTime utcNow, CancellationToken cancellationToken)
    {
        var idempotencyKey = LoyaltyPointLedgerEntry.ComputeIdempotencyKey(request.SourceType, request.SourceId, request.UserId, request.PointType, request.EntryType);

        if (_entries.Values.Any(e => e.IdempotencyKey == idempotencyKey))
        {
            return Task.FromResult<LoyaltyPointLedgerEntry?>(null);
        }

        var account = _accountRepository.GetByIdAsync(request.AccountId, cancellationToken).Result;
        var pointsToDebit = request.Points;

        if (request.EntryType == LoyaltyLedgerEntryType.Expire)
        {
            // Same rule as the real repository: derive the amount from the lot's own remaining
            // balance, never the account's aggregate balance (Module 7 audit finding #2).
            if (!_entries.TryGetValue(request.SourceId, out var lot) || lot.RemainingAmount is null)
            {
                return Task.FromResult<LoyaltyPointLedgerEntry?>(null);
            }

            pointsToDebit = lot.RemainingAmount.Value;
            lot.ReduceRemaining(pointsToDebit);
        }

        var currentBalance = account is null ? 0 : (request.PointType == LoyaltyPointType.RewardPoints ? account.CurrentRewardPoints : account.CurrentStatusPoints);

        if (account is null || currentBalance < pointsToDebit)
        {
            return Task.FromResult<LoyaltyPointLedgerEntry?>(null);
        }

        int balanceAfter;

        if (request.PointType == LoyaltyPointType.RewardPoints)
        {
            account.ApplyRewardPointsDelta(-pointsToDebit, utcNow);
            balanceAfter = account.CurrentRewardPoints;
        }
        else
        {
            account.ApplyStatusPointsDelta(-pointsToDebit, [], utcNow);
            balanceAfter = account.CurrentStatusPoints;
        }

        if (request.PointType == LoyaltyPointType.RewardPoints && request.EntryType != LoyaltyLedgerEntryType.Expire)
        {
            ConsumeLotsFifo(request.AccountId, pointsToDebit);
        }

        var entry = LoyaltyPointLedgerEntry.Create(
            request.AccountId, request.UserId, request.PointType, request.EntryType, -pointsToDebit, balanceAfter, request.SourceType,
            request.SourceId, request.Reason, utcNow, request.EarningRuleId, request.RewardId, request.ReferralId, request.ExpirationAtUtc,
            request.CreatedBy);

        _entries[entry.Id] = entry;
        return Task.FromResult<LoyaltyPointLedgerEntry?>(entry);
    }

    /// <summary>Mirrors LoyaltyRewardPointsLotConsumer: earliest-expiring lot first, never touching a lot beyond what it still has remaining.</summary>
    private void ConsumeLotsFifo(Guid accountId, int amount)
    {
        var remaining = amount;

        var lots = _entries.Values
            .Where(e => e.LoyaltyAccountId == accountId && e.PointType == LoyaltyPointType.RewardPoints
                && e.EntryType == LoyaltyLedgerEntryType.Earn && e.RemainingAmount > 0)
            .OrderBy(e => e.ExpirationAtUtc == null)
            .ThenBy(e => e.ExpirationAtUtc)
            .ThenBy(e => e.CreatedAtUtc);

        foreach (var lot in lots)
        {
            if (remaining <= 0)
            {
                break;
            }

            var take = Math.Min(lot.RemainingAmount!.Value, remaining);
            lot.ReduceRemaining(take);
            remaining -= take;
        }
    }
}
