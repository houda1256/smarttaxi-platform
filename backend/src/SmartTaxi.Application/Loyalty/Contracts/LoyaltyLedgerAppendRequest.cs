using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Loyalty.Contracts;

/// <summary>
/// What a Loyalty Application handler hands to ILoyaltyPointLedgerRepository's
/// atomic credit/debit operations. Points is always given as a positive
/// magnitude here — TryCreditAsync applies it as a gain, TryDebitAsync applies
/// it as a conditional loss; the ledger row itself stores the correctly signed
/// value (see LoyaltyPointLedgerEntry).
/// </summary>
public sealed record LoyaltyLedgerAppendRequest(
    Guid AccountId,
    Guid UserId,
    LoyaltyPointType PointType,
    LoyaltyLedgerEntryType EntryType,
    int Points,
    string SourceType,
    Guid SourceId,
    string Reason,
    Guid? EarningRuleId = null,
    Guid? RewardId = null,
    Guid? ReferralId = null,
    DateTime? ExpirationAtUtc = null,
    Guid? CreatedBy = null);
