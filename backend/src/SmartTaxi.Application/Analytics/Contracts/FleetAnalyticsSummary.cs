namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>
/// ActiveVehicles/ActiveDrivers are current-state, not period-scoped (same
/// convention as the dashboard). PartnerGrowthCount counts only
/// GaragePartner/RoadsideAssistancePartner approvals (approved decision) —
/// TaxiOwner (Fleet's own concern) and Advertiser (has its own CampaignGrowth)
/// are deliberately excluded.
/// </summary>
public sealed record FleetAnalyticsSummary(
    DateTime FromUtc,
    DateTime ToUtc,
    int ActiveVehicles,
    int VehicleGrowthCount,
    int ActiveDrivers,
    int PartnerGrowthCount);
