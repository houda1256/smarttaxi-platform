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
    Reversal,

    /// <summary>Advertiser owes the Platform for a campaign's settled consumed budget — see Module 8's Advertising integration seam (AdvertisingBillingService). Added additively; no existing Payments business rule was changed.</summary>
    AdvertisingRevenue,

    /// <summary>TaxiOwner owes the GaragePartner for a completed maintenance request's final cost — see Module 9's Maintenance integration seam (MaintenanceBillingService). Added additively; no existing Payments business rule was changed.</summary>
    MaintenanceRevenue,

    /// <summary>The requester (TaxiOwner or Driver) owes the RoadsideAssistancePartner for a completed intervention's final cost — see Module 10's Roadside Assistance integration seam (RoadsideAssistanceBillingService). Added additively; no existing Payments business rule was changed.</summary>
    RoadsideAssistanceRevenue
}
