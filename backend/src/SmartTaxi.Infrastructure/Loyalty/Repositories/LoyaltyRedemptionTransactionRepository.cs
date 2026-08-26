using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Infrastructure.Loyalty;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

/// <summary>
/// One DB transaction spans the idempotency check, the points debit, the
/// reward's usage-limit reservation, and the LoyaltyRedemption insert (see
/// ILoyaltyRedemptionTransactionRepository) — a lost usage-limit race rolls
/// the whole attempt back, so the debit is never left stranded and no
/// compensating refund is needed. A retry with the same (UserId,
/// IdempotencyKey) never re-debits: it is detected up front and again, under
/// concurrency, by the unique index on (UserId, IdempotencyKey) itself.
/// </summary>
internal sealed class LoyaltyRedemptionTransactionRepository : ILoyaltyRedemptionTransactionRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyRedemptionTransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LoyaltyRedemptionAttempt> TryRedeemAsync(
        Guid userId, Guid accountId, Guid rewardId, int costInRewardPoints, string idempotencyKey, string reason, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _context.LoyaltyRedemptions
            .FirstOrDefaultAsync(redemption => redemption.UserId == userId && redemption.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            return new LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome.Replayed, existing);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var debitRows = await _context.LoyaltyAccounts
            .Where(account => account.Id == accountId && account.CurrentRewardPoints >= costInRewardPoints)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(account => account.CurrentRewardPoints, account => account.CurrentRewardPoints - costInRewardPoints)
                .SetProperty(account => account.UpdatedAtUtc, utcNow), cancellationToken);

        if (debitRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome.InsufficientBalance, null);
        }

        var usageLimitRows = await _context.LoyaltyRewards
            .Where(reward => reward.Id == rewardId && (reward.UsageLimit == null || reward.RedeemedCount < reward.UsageLimit))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(reward => reward.RedeemedCount, reward => reward.RedeemedCount + 1)
                .SetProperty(reward => reward.UpdatedAtUtc, utcNow), cancellationToken);

        if (usageLimitRows != 1)
        {
            // The transaction is rolled back entirely — the debit above is undone with it, so no
            // compensating refund is needed (Module 7 audit fix #4).
            await transaction.RollbackAsync(cancellationToken);
            return new LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome.UsageLimitReached, null);
        }

        await LoyaltyRewardPointsLotConsumer.ConsumeAsync(_context, accountId, costInRewardPoints, cancellationToken);

        var account = await _context.LoyaltyAccounts.AsNoTracking().FirstAsync(a => a.Id == accountId, cancellationToken);
        var redemption = LoyaltyRedemption.Create(accountId, userId, rewardId, costInRewardPoints, idempotencyKey, utcNow);

        var ledgerEntry = LoyaltyPointLedgerEntry.Create(
            accountId, userId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Redeem, -costInRewardPoints,
            account.CurrentRewardPoints, "Redemption", redemption.Id, reason, utcNow);

        try
        {
            await _context.LoyaltyRedemptions.AddAsync(redemption, cancellationToken);
            await _context.LoyaltyPointLedgerEntries.AddAsync(ledgerEntry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a concurrent race on the same (UserId, IdempotencyKey) — the whole attempt rolls
            // back, and the winner's own redemption (already committed) is returned as the replay result.
            await transaction.RollbackAsync(cancellationToken);
            var winner = await _context.LoyaltyRedemptions
                .FirstOrDefaultAsync(r => r.UserId == userId && r.IdempotencyKey == idempotencyKey, cancellationToken);
            return new LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome.Replayed, winner);
        }

        await transaction.CommitAsync(cancellationToken);
        return new LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome.Created, redemption);
    }
}
