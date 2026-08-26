namespace SmartTaxi.Domain.Support.Enums;

/// <summary>
/// Closed set of entity types a SupportTicket/SupportIncident may reference —
/// never an arbitrary client-supplied string. Referential integrity for
/// RelatedEntityId is enforced entirely at the Application layer (each type
/// resolves to a specific existing module's own read repository); PostgreSQL
/// cannot and must not be asked to enforce a polymorphic foreign key here.
/// </summary>
public enum SupportRelatedEntityType
{
    Ride,
    Payment,
    FinancialDispute,
    Subscription,
    AdvertisingCampaign,
    MaintenanceRequest,
    RoadsideAssistanceRequest,
    ProfessionalAccountRequest,
    Vehicle
}
