using SmartTaxi.Application.Common;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Abstractions;

public interface IRoadsideAssistanceRequestRepository
{
    /// <summary>Returns false (instead of throwing) if the partial unique index (VehicleId, WHERE Status is non-terminal) rejects a concurrent duplicate — the index is the real, race-safe enforcement point; HasActiveRequestForVehicleAsync above is only a cheap pre-check for a friendlier validation error.</summary>
    Task<bool> TryAddAsync(RoadsideAssistanceRequest request, CancellationToken cancellationToken);

    Task<RoadsideAssistanceRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken);

    Task<PagedResult<RoadsideAssistanceRequest>> GetForRequesterAsync(Guid requesterUserId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Every request this partner was ever SelectedPartnerUserId for, regardless of current status — a partner's own job history, never a browsable feed of unrelated requests.</summary>
    Task<PagedResult<RoadsideAssistanceRequest>> GetForPartnerAsync(Guid partnerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<RoadsideAssistanceRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Cheap pre-check only for a friendlier validation error — the partial unique index on (VehicleId) WHERE Status is non-terminal (Rejected counted as active) is the real, race-safe enforcement point.</summary>
    Task<bool> HasActiveRequestForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    /// <summary>
    /// The single atomic conditional transition for every pure-Roadside status
    /// change that never touches a second table — PartnerOnTheWay/
    /// PartnerArrived/requester-Cancel/Reselect(Rejected-&gt;PartnersAvailable,
    /// clearSelectedPartner=true)/Expire/Dispute. Also composed (never
    /// re-implemented) by IRoadsideWorkStartRepository/IRoadsideCompletionRepository/
    /// IRoadsideForceCancelRepository for the transitions that also need a
    /// same-transaction Fleet coordination — this method itself never opens
    /// its own transaction so it can safely participate in an ambient one.
    /// requiredRequesterUserId/requiredPartnerUserId, when non-null, are added
    /// to the WHERE clause — the ownership/assignment guard lives in the same
    /// atomic statement as the status guard, never a separate check. Only the
    /// fields relevant to newStatus are actually written.
    /// </summary>
    Task<bool> TryTransitionAsync(
        Guid requestId, IReadOnlyCollection<RoadsideRequestStatus> allowedFromStatuses, RoadsideRequestStatus newStatus,
        Guid? requiredRequesterUserId, Guid? requiredPartnerUserId, decimal? finalCost, string? reason, Guid? cancelledByUserId,
        bool clearSelectedPartner, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// PartnersAvailable -&gt; PendingPartnerResponse: sets SelectedPartnerUserId
    /// and inserts a new RoadsidePartnerSelectionHistory row (cycle number
    /// computed as the next one for this request) in one transaction — never
    /// two independently committed writes.
    /// </summary>
    Task<bool> TrySelectPartnerAsync(Guid requestId, Guid requesterUserId, Guid partnerUserId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// PendingPartnerResponse -&gt; Accepted/Rejected: transitions the request AND
    /// resolves the single currently-pending RoadsidePartnerSelectionHistory
    /// row (WHERE Response IS NULL — at most one such row can exist per
    /// request by construction) in one transaction.
    /// </summary>
    Task<bool> TryRespondAsync(
        Guid requestId, Guid partnerUserId, RoadsidePartnerResponse response, decimal? estimatedCost, string? rejectionReason, DateTime utcNow,
        CancellationToken cancellationToken);

    Task<bool> TryMarkSettledAsync(Guid requestId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Candidates for the manual expire-sweep — PartnersAvailable/PendingPartnerResponse/Rejected requests whose RequestedAtUtc predates the staleness threshold. No scheduler exists in this codebase; the sweep is always externally/admin-triggered, same convention as Fleet's expire-sweep and Subscriptions' expire-due.</summary>
    Task<IReadOnlyCollection<RoadsideAssistanceRequest>> GetStaleForExpiryAsync(DateTime staleBeforeUtc, CancellationToken cancellationToken);

    /// <summary>Guarded WHERE EscalatedMaintenanceRequestId IS NULL — the idempotency anchor IRoadsideEscalationRepository relies on.</summary>
    Task<bool> TrySetEscalatedMaintenanceRequestIdAsync(Guid requestId, Guid maintenanceRequestId, DateTime utcNow, CancellationToken cancellationToken);
}
