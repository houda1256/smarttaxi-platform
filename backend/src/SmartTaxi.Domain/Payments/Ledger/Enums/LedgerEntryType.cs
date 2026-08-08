namespace SmartTaxi.Domain.Payments.Ledger.Enums;

public enum LedgerEntryType
{
    PaymentCollected,
    PlatformCommission,
    DriverEarning,
    OwnerEarning,
    PartnerEarning,
    Refund,
    Payout,
    CashSettlement,
    Fee,
    Debt,
    Adjustment,
    Reversal
}
