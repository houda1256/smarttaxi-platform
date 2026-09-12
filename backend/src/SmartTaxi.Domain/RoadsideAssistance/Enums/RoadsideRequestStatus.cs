namespace SmartTaxi.Domain.RoadsideAssistance.Enums;

/// <summary>
/// Exact statuses from the business specification's Module 6 (Roadside
/// Assistance) lifecycle. "Requested" and "PartnerSelected" are momentary —
/// RoadsideAssistanceRequest.Create always persists PartnersAvailable
/// directly, and the SelectPartner transition always persists
/// PendingPartnerResponse directly (same "no separate transient row-state"
/// convention MaintenanceRequestStatus/AdCampaign use for their own momentary
/// states) — both values still exist here for completeness/symmetry with the
/// specification's own list.
///
/// Rejected is deliberately NOT terminal: it means the selected partner
/// declined, and the requester may explicitly return to PartnersAvailable to
/// pick another partner (approved design decision) — see
/// RoadsideRequestStatusExtensions.IsTerminal for the authoritative terminal
/// set (Completed, Cancelled, Expired, Disputed).
/// </summary>
public enum RoadsideRequestStatus
{
    Requested,
    PartnersAvailable,
    PartnerSelected,
    PendingPartnerResponse,
    Accepted,
    PartnerOnTheWay,
    PartnerArrived,
    InProgress,
    Completed,
    Cancelled,
    Rejected,
    Expired,
    Disputed
}
