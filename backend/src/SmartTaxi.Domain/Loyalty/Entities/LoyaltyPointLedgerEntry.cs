using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Loyalty.Entities;

/// <summary>
/// Immutable, append-only — same "history row, own table, never updated or
/// deleted" convention as PaymentTransactionHistory/NotificationDeliveryAttempt.
/// Corrections are compensating AdminAdjustment rows, never edits.
/// IdempotencyKey is deterministic from (SourceType, SourceId, UserId,
/// PointType, EntryType) and carries a DB unique index (see
/// LoyaltyPointLedgerEntryConfiguration) — this is the exact "unique earn per
/// source event" guard the spec requires, generalized to every entry kind: a
/// Payment can only ever produce one Earn RewardPoints row and one Earn
/// StatusPoints row for its payer; a Redemption/Challenge/Expiry likewise can
/// only ever produce one row per its own (SourceType, SourceId) pair. UserId is
/// part of the key (not just SourceType/SourceId) because one Referral fans out
/// into two people's ledgers sharing the same SourceId — see the key's own doc
/// comment. Expiring a specific Earn entry uses that entry's own Id as SourceId
/// (SourceType "LoyaltyPointLedgerEntry"), so the same earned batch can never
/// be expired twice — see ProcessExpiredPointsCommand.
/// </summary>
public sealed class LoyaltyPointLedgerEntry : Entity
{
    public Guid LoyaltyAccountId { get; private set; }
    public Guid UserId { get; private set; }
    public LoyaltyPointType PointType { get; private set; }
    public LoyaltyLedgerEntryType EntryType { get; private set; }

    /// <summary>Signed: positive for Earn/ReferralReward/ChallengeReward and credit AdminAdjustment; negative (or zero) for Redeem/Expire and debit AdminAdjustment.</summary>
    public int Points { get; private set; }

    public int BalanceAfter { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public Guid? EarningRuleId { get; private set; }
    public Guid? RewardId { get; private set; }
    public Guid? ReferralId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime? ExpirationAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>
    /// Operational bookkeeping only — NOT part of this row's immutable audit
    /// history (Points/BalanceAfter/Reason/etc. never change after creation).
    /// Populated only for Earn/RewardPoints rows, which are the only rows that
    /// represent a spendable/expirable "lot": starts equal to Points and is
    /// decremented as this specific lot is consumed by redemptions/adjustments
    /// (earliest-expiring lot first) or fully zeroed by its own expiration.
    /// ProcessExpiredPoints reads this to expire exactly what THIS lot still
    /// holds, never the account's aggregate balance — see the Module 7 audit
    /// finding #2 this field exists to fix (a multi-lot account could
    /// otherwise have one lot's expiration incorrectly consume another,
    /// untouched lot's still-valid points).
    /// </summary>
    public int? RemainingAmount { get; private set; }

    private LoyaltyPointLedgerEntry()
    {
    }

    private LoyaltyPointLedgerEntry(
        Guid loyaltyAccountId, Guid userId, LoyaltyPointType pointType, LoyaltyLedgerEntryType entryType, int points, int balanceAfter,
        string sourceType, Guid sourceId, Guid? earningRuleId, Guid? rewardId, Guid? referralId, string reason,
        DateTime? expirationAtUtc, Guid? createdBy, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        LoyaltyAccountId = loyaltyAccountId;
        UserId = userId;
        PointType = pointType;
        EntryType = entryType;
        Points = points;
        BalanceAfter = balanceAfter;
        SourceType = sourceType;
        SourceId = sourceId;
        EarningRuleId = earningRuleId;
        RewardId = rewardId;
        ReferralId = referralId;
        Reason = reason;
        ExpirationAtUtc = expirationAtUtc;
        CreatedBy = createdBy;
        CreatedAtUtc = utcNow;
        IdempotencyKey = ComputeIdempotencyKey(sourceType, sourceId, userId, pointType, entryType);
        RemainingAmount = entryType == LoyaltyLedgerEntryType.Earn && pointType == LoyaltyPointType.RewardPoints ? points : null;
    }

    public static LoyaltyPointLedgerEntry Create(
        Guid loyaltyAccountId, Guid userId, LoyaltyPointType pointType, LoyaltyLedgerEntryType entryType, int points, int balanceAfter,
        string sourceType, Guid sourceId, string reason, DateTime utcNow, Guid? earningRuleId = null, Guid? rewardId = null,
        Guid? referralId = null, DateTime? expirationAtUtc = null, Guid? createdBy = null)
    {
        if (loyaltyAccountId == Guid.Empty)
        {
            throw new ArgumentException("Le compte fidélité est requis.");
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentException("Le type de source est requis.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Un motif est requis pour chaque écriture du grand livre.");
        }

        if (balanceAfter < 0)
        {
            throw new ArgumentException("Le solde résultant ne peut pas être négatif.");
        }

        return new LoyaltyPointLedgerEntry(
            loyaltyAccountId, userId, pointType, entryType, points, balanceAfter, sourceType.Trim(), sourceId, earningRuleId, rewardId,
            referralId, reason.Trim(), expirationAtUtc, createdBy, utcNow);
    }

    /// <summary>
    /// Includes UserId deliberately: one business event (e.g. one Referral)
    /// can fan out into ledger effects for two different people (referrer AND
    /// referee) sharing the same SourceId — without UserId in the key, the
    /// second person's entry would collide with the first's on the unique
    /// index and silently no-op. Scoping the key per (source, person) makes
    /// each person's effect independently idempotent.
    /// </summary>
    public static string ComputeIdempotencyKey(string sourceType, Guid sourceId, Guid userId, LoyaltyPointType pointType, LoyaltyLedgerEntryType entryType) =>
        $"{sourceType}:{sourceId:N}:{userId:N}:{pointType}:{entryType}";

    /// <summary>
    /// Consumes part (or all) of this lot's still-available amount — called
    /// when a redemption/adjustment spends points (earliest-expiring lot
    /// first) or when this lot itself expires. The repository layer performs
    /// the actual concurrency-safe mutation via an atomic conditional UPDATE
    /// (see LoyaltyRewardPointsLotConsumer); this method exists so the
    /// invariant itself — a lot's remaining amount can never go negative or be
    /// reduced past zero — is directly unit-testable.
    /// </summary>
    public void ReduceRemaining(int amount)
    {
        if (RemainingAmount is null)
        {
            throw new InvalidOperationException("Cette écriture ne représente pas un lot de points consommable.");
        }

        if (amount < 0 || amount > RemainingAmount)
        {
            throw new ArgumentException("Le montant à retirer de ce lot est invalide.");
        }

        RemainingAmount -= amount;
    }
}
