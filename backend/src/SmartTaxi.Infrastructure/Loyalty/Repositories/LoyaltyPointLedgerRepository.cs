using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Infrastructure.Loyalty;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

/// <summary>
/// TryCreditAsync/TryDebitAsync bundle the account balance mutation and the
/// ledger insert into one explicit database transaction (same convention as
/// RideRepository.TryCompleteAsync). The balance mutation itself is a
/// server-evaluated increment/decrement (SetProperty(x => x.Field, x =>
/// x.Field + delta)), which Postgres executes under the row's own lock — safe
/// under concurrency without any separate SELECT ... FOR UPDATE. The
/// idempotency-key existence check happens both up front (avoids wasted work
/// on an obvious duplicate) and implicitly again via the ledger table's own
/// unique index (the real guarantee under true concurrent races — see
/// SubscriptionRepository.TryAddAsync for the same two-layer pattern).
/// </summary>
internal sealed class LoyaltyPointLedgerRepository : ILoyaltyPointLedgerRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyPointLedgerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LoyaltyPointLedgerEntry?> GetByIdAsync(Guid entryId, CancellationToken cancellationToken) =>
        _context.LoyaltyPointLedgerEntries.FirstOrDefaultAsync(entry => entry.Id == entryId, cancellationToken);

    public Task<LoyaltyPointLedgerEntry?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        _context.LoyaltyPointLedgerEntries.FirstOrDefaultAsync(entry => entry.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<PagedResult<LoyaltyPointLedgerEntry>> GetForUserAsync(
        Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.LoyaltyPointLedgerEntries.Where(entry => entry.UserId == userId);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LoyaltyPointLedgerEntry>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyCollection<LoyaltyPointLedgerEntry>> GetDueForExpirationAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var candidates = await _context.LoyaltyPointLedgerEntries
            .Where(entry => entry.PointType == LoyaltyPointType.RewardPoints && entry.EntryType == LoyaltyLedgerEntryType.Earn
                && entry.ExpirationAtUtc != null && entry.ExpirationAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return [];
        }

        var candidateExpiryKeys = candidates
            .Select(entry => LoyaltyPointLedgerEntry.ComputeIdempotencyKey(
                "LoyaltyPointLedgerEntry", entry.Id, entry.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire))
            .ToList();

        var alreadyExpiredKeys = (await _context.LoyaltyPointLedgerEntries
                .Where(entry => candidateExpiryKeys.Contains(entry.IdempotencyKey))
                .Select(entry => entry.IdempotencyKey)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return candidates
            .Where(entry => !alreadyExpiredKeys.Contains(
                LoyaltyPointLedgerEntry.ComputeIdempotencyKey(
                    "LoyaltyPointLedgerEntry", entry.Id, entry.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire)))
            .ToList();
    }

    public Task<int> CountEntriesAsync(Guid userId, string sourceType, LoyaltyLedgerEntryType entryType, CancellationToken cancellationToken) =>
        _context.LoyaltyPointLedgerEntries.CountAsync(
            entry => entry.UserId == userId && entry.SourceType == sourceType && entry.EntryType == entryType, cancellationToken);

    public async Task<LoyaltyPointLedgerEntry?> TryCreditAsync(
        LoyaltyLedgerAppendRequest request, IReadOnlyCollection<LoyaltyTierThreshold> statusTierThresholds, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = LoyaltyPointLedgerEntry.ComputeIdempotencyKey(
            request.SourceType, request.SourceId, request.UserId, request.PointType, request.EntryType);

        if (await _context.LoyaltyPointLedgerEntries.AnyAsync(entry => entry.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            return null;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (request.PointType == LoyaltyPointType.RewardPoints)
        {
            await _context.LoyaltyAccounts.Where(account => account.Id == request.AccountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.CurrentRewardPoints, account => account.CurrentRewardPoints + request.Points)
                    .SetProperty(account => account.UpdatedAtUtc, utcNow), cancellationToken);
        }
        else
        {
            await _context.LoyaltyAccounts.Where(account => account.Id == request.AccountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.CurrentStatusPoints, account => account.CurrentStatusPoints + request.Points)
                    .SetProperty(account => account.UpdatedAtUtc, utcNow), cancellationToken);
        }

        var account = await _context.LoyaltyAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var balanceAfter = request.PointType == LoyaltyPointType.RewardPoints ? account.CurrentRewardPoints : account.CurrentStatusPoints;

        if (request.PointType == LoyaltyPointType.StatusPoints)
        {
            var newTier = LoyaltyTierCalculator.Determine(account.CurrentStatusPoints, statusTierThresholds);

            if (newTier != account.Tier)
            {
                await _context.LoyaltyAccounts.Where(a => a.Id == request.AccountId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Tier, newTier), cancellationToken);
            }
        }

        var entry = LoyaltyPointLedgerEntry.Create(
            request.AccountId, request.UserId, request.PointType, request.EntryType, request.Points, balanceAfter, request.SourceType,
            request.SourceId, request.Reason, utcNow, request.EarningRuleId, request.RewardId, request.ReferralId, request.ExpirationAtUtc,
            request.CreatedBy);

        try
        {
            await _context.LoyaltyPointLedgerEntries.AddAsync(entry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await transaction.CommitAsync(cancellationToken);
        return entry;
    }

    public async Task<LoyaltyPointLedgerEntry?> TryDebitAsync(LoyaltyLedgerAppendRequest request, DateTime utcNow, CancellationToken cancellationToken)
    {
        var idempotencyKey = LoyaltyPointLedgerEntry.ComputeIdempotencyKey(
            request.SourceType, request.SourceId, request.UserId, request.PointType, request.EntryType);

        if (await _context.LoyaltyPointLedgerEntries.AnyAsync(entry => entry.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            return null;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var pointsToDebit = request.Points;
        int? expireLotCurrentRemaining = null;

        if (request.EntryType == LoyaltyLedgerEntryType.Expire)
        {
            // Authoritative amount comes from THIS lot's own remaining balance, never the caller-supplied
            // Points nor the account's aggregate balance (Module 7 audit finding #2). Only a plain read
            // here (no lock taken yet) — the account row is locked first below, same order as the
            // Redeem/AdminAdjustment path's account-then-lot locking, so the two paths can never deadlock
            // against each other under concurrency.
            expireLotCurrentRemaining = await _context.LoyaltyPointLedgerEntries.AsNoTracking()
                .Where(entry => entry.Id == request.SourceId)
                .Select(entry => entry.RemainingAmount)
                .FirstOrDefaultAsync(cancellationToken);

            if (expireLotCurrentRemaining is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            pointsToDebit = expireLotCurrentRemaining.Value;
        }

        int rows;

        if (request.PointType == LoyaltyPointType.RewardPoints)
        {
            rows = await _context.LoyaltyAccounts
                .Where(account => account.Id == request.AccountId && account.CurrentRewardPoints >= pointsToDebit)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.CurrentRewardPoints, account => account.CurrentRewardPoints - pointsToDebit)
                    .SetProperty(account => account.UpdatedAtUtc, utcNow), cancellationToken);
        }
        else
        {
            rows = await _context.LoyaltyAccounts
                .Where(account => account.Id == request.AccountId && account.CurrentStatusPoints >= pointsToDebit)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.CurrentStatusPoints, account => account.CurrentStatusPoints - pointsToDebit)
                    .SetProperty(account => account.UpdatedAtUtc, utcNow), cancellationToken);
        }

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        if (request.EntryType == LoyaltyLedgerEntryType.Expire)
        {
            // Optimistic conditional UPDATE keyed on the exact value just read — if a concurrent consumer
            // (a redemption via LoyaltyRewardPointsLotConsumer) already changed this lot, this affects 0
            // rows and the whole attempt (including the account decrement above) rolls back, safe to retry
            // on the next sweep.
            var lotRows = await _context.LoyaltyPointLedgerEntries
                .Where(entry => entry.Id == request.SourceId && entry.RemainingAmount == expireLotCurrentRemaining)
                .ExecuteUpdateAsync(setters => setters.SetProperty(entry => entry.RemainingAmount, 0), cancellationToken);

            if (lotRows != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }
        }
        else if (request.PointType == LoyaltyPointType.RewardPoints)
        {
            // A genuine spend (Redeem/AdminAdjustment) — attribute it to the earliest-expiring lot(s), locked
            // in the same account-then-lot order, so ProcessExpiredPoints later expires only what each lot truly has left.
            await LoyaltyRewardPointsLotConsumer.ConsumeAsync(_context, request.AccountId, pointsToDebit, cancellationToken);
        }

        var account = await _context.LoyaltyAccounts.AsNoTracking().FirstAsync(a => a.Id == request.AccountId, cancellationToken);
        var balanceAfter = request.PointType == LoyaltyPointType.RewardPoints ? account.CurrentRewardPoints : account.CurrentStatusPoints;

        var entry = LoyaltyPointLedgerEntry.Create(
            request.AccountId, request.UserId, request.PointType, request.EntryType, -pointsToDebit, balanceAfter, request.SourceType,
            request.SourceId, request.Reason, utcNow, request.EarningRuleId, request.RewardId, request.ReferralId, request.ExpirationAtUtc,
            request.CreatedBy);

        try
        {
            await _context.LoyaltyPointLedgerEntries.AddAsync(entry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await transaction.CommitAsync(cancellationToken);
        return entry;
    }
}
