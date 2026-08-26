using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty;

/// <summary>
/// Shared by LoyaltyPointLedgerRepository.TryDebitAsync (AdminAdjustment
/// debits) and the redemption transaction repository: whenever RewardPoints
/// are actually spent, decrements the specific earning lots (Earn ledger rows)
/// that balance was drawn from — earliest-expiring lot first (FEFO), so
/// ProcessExpiredPoints can later expire exactly what each lot has left
/// instead of inferring it from the account's aggregate balance (Module 7
/// audit finding #2). Must be called within the caller's own open transaction
/// on the same DbContext — it does not manage its own transaction.
///
/// Each lot's decrement is an optimistic, conditional UPDATE keyed on the
/// exact remaining value just read (WHERE RemainingAmount == currentRemaining)
/// — the same "atomic conditional update" idiom used everywhere else in this
/// codebase, just applied per-lot instead of per-account. If a concurrent
/// consumer or the lot's own expiration wins the race, this simply moves on
/// with a fresh read (bounded retries) rather than overwriting a stale value.
/// </summary>
internal static class LoyaltyRewardPointsLotConsumer
{
    private const int MaxAttemptsPerLot = 3;

    public static async Task ConsumeAsync(ApplicationDbContext context, Guid accountId, int amountToConsume, CancellationToken cancellationToken)
    {
        if (amountToConsume <= 0)
        {
            return;
        }

        var remainingToConsume = amountToConsume;

        var candidateLotIds = await context.LoyaltyPointLedgerEntries
            .Where(entry => entry.LoyaltyAccountId == accountId && entry.PointType == LoyaltyPointType.RewardPoints
                && entry.EntryType == LoyaltyLedgerEntryType.Earn && entry.RemainingAmount > 0)
            .OrderBy(entry => entry.ExpirationAtUtc == null)
            .ThenBy(entry => entry.ExpirationAtUtc)
            .ThenBy(entry => entry.CreatedAtUtc)
            .Select(entry => entry.Id)
            .ToListAsync(cancellationToken);

        foreach (var lotId in candidateLotIds)
        {
            if (remainingToConsume <= 0)
            {
                break;
            }

            for (var attempt = 0; attempt < MaxAttemptsPerLot && remainingToConsume > 0; attempt++)
            {
                var currentRemaining = await context.LoyaltyPointLedgerEntries.AsNoTracking()
                    .Where(entry => entry.Id == lotId)
                    .Select(entry => entry.RemainingAmount)
                    .FirstOrDefaultAsync(cancellationToken);

                if (currentRemaining is null or <= 0)
                {
                    break;
                }

                var toTake = Math.Min(currentRemaining.Value, remainingToConsume);

                var rows = await context.LoyaltyPointLedgerEntries
                    .Where(entry => entry.Id == lotId && entry.RemainingAmount == currentRemaining)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(entry => entry.RemainingAmount, entry => entry.RemainingAmount!.Value - toTake), cancellationToken);

                if (rows == 1)
                {
                    remainingToConsume -= toTake;
                    break;
                }

                // Lost the race to a concurrent consumer/expirer of this same lot — retry with a fresh read.
            }
        }

        // If remainingToConsume > 0 here, the spend exceeded the sum of tracked Earn lots (e.g. some of
        // the balance came from ReferralReward/ChallengeReward credits, which are never lot-tracked
        // because they never expire). That is expected and harmless — those points simply can't expire.
    }
}
