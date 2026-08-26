using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of the real repository's atomic debit+usage-limit+redemption transaction — checks both conditions before applying either mutation, so a failure never leaves partial state, matching the real transaction's all-or-nothing guarantee.</summary>
public sealed class FakeLoyaltyRedemptionTransactionRepository : ILoyaltyRedemptionTransactionRepository
{
    private readonly FakeLoyaltyAccountRepository _accountRepository;
    private readonly FakeLoyaltyRewardRepository _rewardRepository;
    private readonly Dictionary<Guid, LoyaltyRedemption> _redemptions = new();

    public FakeLoyaltyRedemptionTransactionRepository(FakeLoyaltyAccountRepository accountRepository, FakeLoyaltyRewardRepository rewardRepository)
    {
        _accountRepository = accountRepository;
        _rewardRepository = rewardRepository;
    }

    public async Task<LoyaltyRedemptionAttempt> TryRedeemAsync(
        Guid userId, Guid accountId, Guid rewardId, int costInRewardPoints, string idempotencyKey, string reason, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var existing = _redemptions.Values.FirstOrDefault(r => r.UserId == userId && r.IdempotencyKey == idempotencyKey);

        if (existing is not null)
        {
            return new LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome.Replayed, existing);
        }

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);

        if (account is null || account.CurrentRewardPoints < costInRewardPoints)
        {
            return new LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome.InsufficientBalance, null);
        }

        if (!await _rewardRepository.TryIncrementRedeemedCountAsync(rewardId, utcNow, cancellationToken))
        {
            return new LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome.UsageLimitReached, null);
        }

        account.ApplyRewardPointsDelta(-costInRewardPoints, utcNow);

        var redemption = LoyaltyRedemption.Create(accountId, userId, rewardId, costInRewardPoints, idempotencyKey, utcNow);
        _redemptions[redemption.Id] = redemption;

        return new LoyaltyRedemptionAttempt(LoyaltyRedemptionAttemptOutcome.Created, redemption);
    }
}
