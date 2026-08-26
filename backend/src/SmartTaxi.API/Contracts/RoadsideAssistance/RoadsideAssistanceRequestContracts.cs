using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.API.Contracts.RoadsideAssistance;

public sealed record CreateRoadsideAssistanceRequestRequest(
    RoadsideRequesterRole RequesterRole, Guid VehicleId, Guid? RideId, RoadsideServiceType ServiceType, RoadsideUrgency Urgency,
    string Description, double Latitude, double Longitude, string? Address, string? City);

public sealed record SelectRoadsidePartnerRequest(Guid PartnerUserId);

public sealed record AcceptRoadsideJobRequest(decimal? EstimatedCost);

public sealed record RejectRoadsideJobRequest(string RejectionReason);

public sealed record CancelRoadsideAssistanceRequestRequest(string? Reason);

public sealed record CompleteRoadsideInterventionRequest(decimal FinalCost);

public sealed record ForceCancelRoadsideRequestRequest(string Reason);

public sealed record EscalateRoadsideRequestToMaintenanceRequest(Guid GarageUserId, string? Description);

public sealed record RecommendedRoadsidePartnerResponse(Guid PartnerUserId, string BusinessName, string? City, double? DistanceKm)
{
    public static RecommendedRoadsidePartnerResponse FromDto(RecommendedRoadsidePartner partner) =>
        new(partner.PartnerUserId, partner.BusinessName, partner.City, partner.DistanceKm);
}

public sealed record RoadsideAssistanceRequestResponse(
    Guid Id, Guid RequesterUserId, string RequesterRole, Guid VehicleId, Guid? RideId, string ServiceType, string Urgency, string Description,
    double Latitude, double Longitude, string? Address, string? City, string Status, Guid? SelectedPartnerUserId, decimal? EstimatedCost,
    decimal? FinalCost, DateTime RequestedAtUtc, DateTime? AcceptedAtUtc, DateTime? PartnerOnTheWayAtUtc, DateTime? PartnerArrivedAtUtc,
    DateTime? StartedAtUtc, DateTime? CompletedAtUtc, DateTime? CancelledAtUtc, DateTime? ExpiredAtUtc, DateTime? DisputedAtUtc,
    DateTime? SettledAtUtc, string? CancellationReason, Guid? CancelledByUserId, Guid? EscalatedMaintenanceRequestId, DateTime UpdatedAtUtc)
{
    public static RoadsideAssistanceRequestResponse FromEntity(RoadsideAssistanceRequest request) => new(
        request.Id, request.RequesterUserId, request.RequesterRole.ToString(), request.VehicleId, request.RideId,
        request.ServiceType.ToString(), request.Urgency.ToString(), request.Description, request.Latitude, request.Longitude,
        request.Address, request.City, request.Status.ToString(), request.SelectedPartnerUserId, request.EstimatedCost, request.FinalCost,
        request.RequestedAtUtc, request.AcceptedAtUtc, request.PartnerOnTheWayAtUtc, request.PartnerArrivedAtUtc, request.StartedAtUtc,
        request.CompletedAtUtc, request.CancelledAtUtc, request.ExpiredAtUtc, request.DisputedAtUtc, request.SettledAtUtc,
        request.CancellationReason, request.CancelledByUserId, request.EscalatedMaintenanceRequestId, request.UpdatedAtUtc);
}
