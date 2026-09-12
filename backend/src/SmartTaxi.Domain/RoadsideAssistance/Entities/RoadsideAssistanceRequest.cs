using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Events;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Domain.RoadsideAssistance.Entities;

/// <summary>
/// The single aggregate carrying the entire roadside-assistance lifecycle —
/// request, partner recommendation/selection, response, intervention,
/// completion — same "one unified flow, not several separate entities" shape
/// as MaintenanceRequest. Status transitions are deliberately NOT modeled as
/// mutating methods here — they are enforced as atomic, race-safe conditional
/// SQL updates in the repository (same convention as MaintenanceRequest/
/// AdCampaign), so the guard condition has exactly one source of truth. This
/// entity only owns field-level construction validation. VehicleId/RideId/
/// RequesterUserId/SelectedPartnerUserId are plain Guid references (no FK),
/// same convention every other module uses for referencing Fleet/Identity/
/// Rides. Money fields are decimal only, never float/double. Latitude/
/// Longitude are validated once at construction via GeoCoordinate.Create
/// (reusing Rides' existing range-check logic) but stored as raw doubles per
/// the approved field list — never a nested owned GeoCoordinate navigation,
/// to avoid EF owned-type coupling to another module's Domain type.
/// </summary>
public sealed class RoadsideAssistanceRequest : AggregateRoot
{
    public Guid RequesterUserId { get; private set; }
    public RoadsideRequesterRole RequesterRole { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid? RideId { get; private set; }
    public RoadsideServiceType ServiceType { get; private set; }
    public RoadsideUrgency Urgency { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }

    public RoadsideRequestStatus Status { get; private set; }
    public Guid? SelectedPartnerUserId { get; private set; }

    public decimal? EstimatedCost { get; private set; }

    /// <summary>Financial fact, not operational business data — only set at Completed, drives Finance settlement.</summary>
    public decimal? FinalCost { get; private set; }

    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? PartnerOnTheWayAtUtc { get; private set; }
    public DateTime? PartnerArrivedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime? ExpiredAtUtc { get; private set; }
    public DateTime? DisputedAtUtc { get; private set; }

    /// <summary>Financial fact — the settlement marker (Module 8/9's proven recovery-safe pattern), never operational data.</summary>
    public DateTime? SettledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    /// <summary>Distinguishes the requester's own cancellation from an admin's forced one for audit purposes — matches either RequesterUserId or an admin id.</summary>
    public Guid? CancelledByUserId { get; private set; }

    /// <summary>Set exactly once when EscalateToMaintenance succeeds — also the idempotency guard preventing a retry from creating a second MaintenanceRequest.</summary>
    public Guid? EscalatedMaintenanceRequestId { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private RoadsideAssistanceRequest()
    {
    }

    private RoadsideAssistanceRequest(
        Guid requesterUserId, RoadsideRequesterRole requesterRole, Guid vehicleId, Guid? rideId, RoadsideServiceType serviceType,
        RoadsideUrgency urgency, string description, double latitude, double longitude, string? address, string? city, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        RequesterUserId = requesterUserId;
        RequesterRole = requesterRole;
        VehicleId = vehicleId;
        RideId = rideId;
        ServiceType = serviceType;
        Urgency = urgency;
        Description = description;
        Latitude = latitude;
        Longitude = longitude;
        Address = address;
        City = city;
        // "Requested" is the momentary creation state (see RoadsideRequestStatus doc comment) — the
        // persisted row starts directly at PartnersAvailable, same convention as MaintenanceRequest starting at PendingGarageResponse.
        Status = RoadsideRequestStatus.PartnersAvailable;
        RequestedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        RaiseDomainEvent(new RoadsideAssistanceRequested(Id, requesterUserId, vehicleId, utcNow));
    }

    public static RoadsideAssistanceRequest Create(
        Guid requesterUserId, RoadsideRequesterRole requesterRole, Guid vehicleId, Guid? rideId, RoadsideServiceType serviceType,
        RoadsideUrgency urgency, string description, double latitude, double longitude, string? address, string? city, DateTime utcNow)
    {
        ValidateFields(requesterUserId, vehicleId, description);
        GeoCoordinate.Create(latitude, longitude);

        return new RoadsideAssistanceRequest(
            requesterUserId, requesterRole, vehicleId, rideId, serviceType, urgency, description.Trim(), latitude, longitude,
            address?.Trim(), string.IsNullOrWhiteSpace(city) ? null : city.Trim().ToUpperInvariant(), utcNow);
    }

    private static void ValidateFields(Guid requesterUserId, Guid vehicleId, string description)
    {
        if (requesterUserId == Guid.Empty)
        {
            throw new ArgumentException("Le demandeur est requis.");
        }

        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException("Le véhicule est requis.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("La description de la demande est requise.");
        }
    }
}
