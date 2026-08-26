using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Domain.Loyalty.Events;

namespace SmartTaxi.Domain.Loyalty.Entities;

using SmartTaxi.Domain.Loyalty;

/// <summary>
/// One account per (eligible) user — Customer or Driver only, per current
/// scope. CurrentRewardPoints/CurrentStatusPoints are a cached projection of
/// the immutable LoyaltyPointLedgerEntry history, never the source of truth.
/// The mutation methods here are pure domain math, unit-testable without a
/// database; the real, concurrency-safe mutation is an atomic conditional
/// UPDATE at the repository layer (same "cached counter guarded by an atomic
/// UPDATE" convention as LoyaltyReward.RedeemedCount, Subscription.EndDate,
/// and Payment's balance-affecting fields) — see LoyaltyAccountRepository.
/// </summary>
public sealed class LoyaltyAccount : AggregateRoot
{
    public Guid UserId { get; private set; }
    public UserRole ActorRole { get; private set; }
    public int CurrentRewardPoints { get; private set; }
    public int CurrentStatusPoints { get; private set; }
    public LoyaltyTier Tier { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private LoyaltyAccount()
    {
    }

    private LoyaltyAccount(Guid userId, UserRole actorRole, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        ActorRole = actorRole;
        CurrentRewardPoints = 0;
        CurrentStatusPoints = 0;
        Tier = LoyaltyTier.Bronze;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        RaiseDomainEvent(new LoyaltyAccountCreated(Id, userId, utcNow));
    }

    /// <summary>Eligible actors per current scope: Customer and Driver only — see the audit's explicit scope decision.</summary>
    public static LoyaltyAccount Open(Guid userId, UserRole actorRole, DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("L'utilisateur est requis.");
        }

        if (actorRole is not (UserRole.Customer or UserRole.Driver))
        {
            throw new ArgumentException("Seuls les clients et chauffeurs peuvent avoir un compte fidélité.");
        }

        return new LoyaltyAccount(userId, actorRole, utcNow);
    }

    public void ApplyEarn(int rewardPoints, int statusPoints, IReadOnlyCollection<LoyaltyTierThreshold> thresholds, DateTime utcNow)
    {
        if (rewardPoints < 0 || statusPoints < 0)
        {
            throw new ArgumentException("Les points gagnés ne peuvent pas être négatifs.");
        }

        CurrentRewardPoints += rewardPoints;
        CurrentStatusPoints += statusPoints;
        UpdatedAtUtc = utcNow;
        RecalculateTier(thresholds, utcNow);
    }

    /// <summary>Applies any signed change to the spendable balance — positive for ReferralReward/ChallengeReward/AdminAdjustment credits, negative for Redeem/Expire/AdminAdjustment debits. Never allows the balance below zero.</summary>
    public void ApplyRewardPointsDelta(int delta, DateTime utcNow)
    {
        var newBalance = CurrentRewardPoints + delta;

        if (newBalance < 0)
        {
            throw new ArgumentException("Le solde de points ne peut pas devenir négatif.");
        }

        CurrentRewardPoints = newBalance;
        UpdatedAtUtc = utcNow;
    }

    public void ApplyStatusPointsDelta(int delta, IReadOnlyCollection<LoyaltyTierThreshold> thresholds, DateTime utcNow)
    {
        var newBalance = CurrentStatusPoints + delta;

        if (newBalance < 0)
        {
            throw new ArgumentException("Le solde de points de statut ne peut pas devenir négatif.");
        }

        CurrentStatusPoints = newBalance;
        UpdatedAtUtc = utcNow;
        RecalculateTier(thresholds, utcNow);
    }

    private void RecalculateTier(IReadOnlyCollection<LoyaltyTierThreshold> thresholds, DateTime utcNow)
    {
        var newTier = LoyaltyTierCalculator.Determine(CurrentStatusPoints, thresholds);

        if (newTier == Tier)
        {
            return;
        }

        var previousTier = Tier;
        Tier = newTier;
        RaiseDomainEvent(new LoyaltyTierChanged(Id, UserId, previousTier, newTier, utcNow));
    }
}
