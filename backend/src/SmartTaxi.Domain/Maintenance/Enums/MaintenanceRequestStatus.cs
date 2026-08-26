namespace SmartTaxi.Domain.Maintenance.Enums;

/// <summary>
/// Exact statuses from the business specification's Module 5 (Maintenance and
/// Garages) appointment lifecycle — none invented, none merged. "Requested" is
/// the momentary creation state; MaintenanceRequest.Create always persists
/// PendingGarageResponse directly (the same "no separate transient row-state"
/// convention AdCampaign uses for Draft), but the value still exists here for
/// completeness/symmetry with the specification's own list.
///
/// Rejected vs QuoteRejected vs Cancelled are deliberately distinct — three
/// different actors, three different audit meanings (approved design
/// decision): Rejected = the selected garage refuses before any quote exists;
/// QuoteRejected = the owner refuses a submitted quote; Cancelled = the owner
/// or an admin withdraws the request.
/// </summary>
public enum MaintenanceRequestStatus
{
    Requested,
    PendingGarageResponse,
    Confirmed,
    QuotePending,
    QuoteSubmitted,
    QuoteAccepted,
    QuoteRejected,
    VehicleReceived,
    InProgress,
    WaitingForParts,
    Completed,
    Cancelled,
    Rejected,
    Disputed
}
