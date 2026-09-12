namespace SmartTaxi.Domain.Support.Enums;

/// <summary>Not enumerated by the business specification — approved minimal MVP taxonomy, deliberately not a full configurable catalog (that remains a future SystemSetting/configurable-references concern, out of scope for Module 11).</summary>
public enum SupportTicketCategory
{
    Billing,
    RideIssue,
    MaintenanceIssue,
    RoadsideIssue,
    SubscriptionIssue,
    AdvertisingIssue,
    AccountIssue,
    TechnicalIssue,
    Other
}
