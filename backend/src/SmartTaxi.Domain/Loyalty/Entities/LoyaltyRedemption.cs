using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Loyalty.Events;

namespace SmartTaxi.Domain.Loyalty.Entities;

/// <summary>
/// Immutable record of one redemption — same "append-only audit row, own
/// AggregateRoot" convention as Payments' Receipt/RefundRecord. IdempotencyKey
/// is client-supplied (unlike the ledger's own deterministic keys, a fresh
/// redemption request has no existing domain event to derive a key from) and
/// carries a DB unique index scoped to (UserId, IdempotencyKey) — see
/// LoyaltyRedemptionConfiguration and the Module 7 audit fix #3: without this,
/// a client retry (timeout, double-tap) could double-spend the user's points
/// because a fresh redemption.Id was generated on every call.
/// </summary>
public sealed class LoyaltyRedemption : AggregateRoot
{
    public Guid LoyaltyAccountId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RewardId { get; private set; }
    public int PointsSpent { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime RedeemedAtUtc { get; private set; }

    private LoyaltyRedemption()
    {
    }

    private LoyaltyRedemption(Guid loyaltyAccountId, Guid userId, Guid rewardId, int pointsSpent, string idempotencyKey, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        LoyaltyAccountId = loyaltyAccountId;
        UserId = userId;
        RewardId = rewardId;
        PointsSpent = pointsSpent;
        IdempotencyKey = idempotencyKey;
        RedeemedAtUtc = utcNow;

        RaiseDomainEvent(new RewardRedeemed(Id, userId, rewardId, utcNow));
    }

    public static LoyaltyRedemption Create(Guid loyaltyAccountId, Guid userId, Guid rewardId, int pointsSpent, string idempotencyKey, DateTime utcNow)
    {
        if (pointsSpent <= 0)
        {
            throw new ArgumentException("Le coût en points doit être positif.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Une clé d'idempotence est requise pour chaque échange.");
        }

        return new LoyaltyRedemption(loyaltyAccountId, userId, rewardId, pointsSpent, idempotencyKey.Trim(), utcNow);
    }
}
