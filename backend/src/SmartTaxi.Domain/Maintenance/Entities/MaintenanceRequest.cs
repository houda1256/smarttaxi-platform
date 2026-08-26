using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Domain.Maintenance.Events;

namespace SmartTaxi.Domain.Maintenance.Entities;

/// <summary>
/// The single aggregate carrying the entire garage-maintenance lifecycle —
/// request, garage response, quote, work, completion — matching how the
/// business specification itself describes one unified appointment flow, not
/// three separate Request/Appointment/WorkOrder entities. Status transitions
/// are deliberately NOT modeled as mutating methods here — they are enforced
/// as atomic, race-safe conditional SQL updates in the repository (same
/// convention as AdCampaign/ProfessionalAccountRequest), so the guard
/// condition has exactly one source of truth. This entity only owns
/// field-level construction validation, which is genuine domain logic
/// independent of concurrency concerns. GarageUserId is fixed at creation —
/// one manually selected garage per request, no reassignment, no broadcast
/// (approved MVP design). VehicleId/OwnerUserId/GarageUserId are plain Guid
/// references (no FK), same convention Loyalty/Advertising use for
/// referencing Fleet/Identity. Money fields are decimal only, never
/// float/double.
/// </summary>
public sealed class MaintenanceRequest : AggregateRoot
{
    public Guid VehicleId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public Guid GarageUserId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public MaintenanceRequestStatus Status { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public string? GarageRejectionReason { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public DateTime? QuoteSubmittedAtUtc { get; private set; }
    public DateTime? QuoteAcceptedAtUtc { get; private set; }
    public DateTime? VehicleReceivedAtUtc { get; private set; }
    public DateTime? WorkStartedAtUtc { get; private set; }

    /// <summary>Financial fact, not operational business data — only set at Completed, drives §J settlement.</summary>
    public decimal? FinalCost { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>Financial fact — the settlement marker (Module 8 audit's proven recovery-safe pattern), never operational data.</summary>
    public DateTime? SettledAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>Distinguishes an owner's own cancellation from an admin's forced one for audit purposes — matches either OwnerUserId or an admin id.</summary>
    public Guid? CancelledByUserId { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private MaintenanceRequest()
    {
    }

    private MaintenanceRequest(Guid vehicleId, Guid ownerUserId, Guid garageUserId, string description, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        VehicleId = vehicleId;
        OwnerUserId = ownerUserId;
        GarageUserId = garageUserId;
        Description = description;
        // "Requested" is the momentary creation state (see MaintenanceRequestStatus doc comment) — the
        // persisted row starts directly at PendingGarageResponse, same convention as AdCampaign starting at Draft.
        Status = MaintenanceRequestStatus.PendingGarageResponse;
        RequestedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        RaiseDomainEvent(new MaintenanceRequestCreated(Id, ownerUserId, garageUserId, utcNow));
    }

    public static MaintenanceRequest Create(Guid vehicleId, Guid ownerUserId, Guid garageUserId, string description, DateTime utcNow)
    {
        ValidateFields(vehicleId, ownerUserId, garageUserId, description);
        return new MaintenanceRequest(vehicleId, ownerUserId, garageUserId, description.Trim(), utcNow);
    }

    private static void ValidateFields(Guid vehicleId, Guid ownerUserId, Guid garageUserId, string description)
    {
        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException("Le véhicule est requis.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("Le propriétaire est requis.");
        }

        if (garageUserId == Guid.Empty)
        {
            throw new ArgumentException("Le garage sélectionné est requis.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("La description de la demande est requise.");
        }
    }
}
