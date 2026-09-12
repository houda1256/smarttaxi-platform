using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

/// <summary>
/// One DB transaction spans the LoyaltyReferralReward insert and both
/// RewardPoints credits (see ILoyaltyReferralGrantRepository) — any failure
/// anywhere in the sequence rolls back everything, so a referral can never be
/// left half-rewarded. Each credit step mirrors LoyaltyPointLedgerRepository's
/// own TryCreditAsync logic (idempotency-key check, conditional balance
/// update, ledger insert) but without owning its own transaction, since this
/// repository owns the single outer transaction instead.
/// </summary>
internal sealed class LoyaltyReferralGrantRepository : ILoyaltyReferralGrantRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyReferralGrantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryGrantAsync(
        LoyaltyReferralReward reward, LoyaltyLedgerAppendRequest? referrerCreditRequest,
        LoyaltyLedgerAppendRequest? refereeCreditRequest, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _context.LoyaltyReferralRewards.AddAsync(reward, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (referrerCreditRequest is not null && !await TryApplyCreditAsync(referrerCreditRequest, utcNow, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (refereeCreditRequest is not null && !await TryApplyCreditAsync(refereeCreditRequest, utcNow, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> TryApplyCreditAsync(LoyaltyLedgerAppendRequest request, DateTime utcNow, CancellationToken cancellationToken)
    {
        var idempotencyKey = LoyaltyPointLedgerEntry.ComputeIdempotencyKey(
            request.SourceType, request.SourceId, request.UserId, request.PointType, request.EntryType);

        if (await _context.LoyaltyPointLedgerEntries.AnyAsync(entry => entry.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            return false;
        }

        var rows = await _context.LoyaltyAccounts.Where(account => account.Id == request.AccountId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(account => account.CurrentRewardPoints, account => account.CurrentRewardPoints + request.Points)
                .SetProperty(account => account.UpdatedAtUtc, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        var account = await _context.LoyaltyAccounts.AsNoTracking().FirstAsync(account => account.Id == request.AccountId, cancellationToken);

        var entry = LoyaltyPointLedgerEntry.Create(
            request.AccountId, request.UserId, request.PointType, request.EntryType, request.Points, account.CurrentRewardPoints,
            request.SourceType, request.SourceId, request.Reason, utcNow, request.EarningRuleId, request.RewardId, request.ReferralId,
            request.ExpirationAtUtc, request.CreatedBy);

        try
        {
            await _context.LoyaltyPointLedgerEntries.AddAsync(entry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return false;
        }

        return true;
    }
}
