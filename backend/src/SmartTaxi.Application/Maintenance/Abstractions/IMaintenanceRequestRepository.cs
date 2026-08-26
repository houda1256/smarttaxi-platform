using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Maintenance.Abstractions;

public interface IMaintenanceRequestRepository
{
    /// <summary>Returns false (instead of throwing) if the partial unique index (VehicleId, WHERE Status is non-terminal) rejects a concurrent duplicate — the index is the real, race-safe enforcement point; HasActiveRequestForVehicleAsync above is only a cheap pre-check for a friendlier validation error.</summary>
    Task<bool> TryAddAsync(MaintenanceRequest request, CancellationToken cancellationToken);

    Task<MaintenanceRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken);

    Task<PagedResult<MaintenanceRequest>> GetForOwnerAsync(Guid ownerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<MaintenanceRequest>> GetForGarageAsync(Guid garageUserId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<MaintenanceRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MaintenanceRequest>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    /// <summary>Cheap pre-check only for a friendlier validation error — the partial unique index on (VehicleId) WHERE Status is non-terminal is the real, race-safe enforcement point.</summary>
    Task<bool> HasActiveRequestForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    /// <summary>
    /// The single atomic conditional transition for every pure-Maintenance status
    /// change (never touches Fleet) — Confirm/RejectGarage/SubmitQuote/
    /// AcceptQuote/RejectQuote/ReceiveVehicle/WaitingForParts toggle/owner-
    /// Cancel. Also composed (never re-implemented) by
    /// IMaintenanceWorkStartRepository/IMaintenanceCompletionRepository/
    /// IMaintenanceForceCancelRepository for the transitions that also need a
    /// same-transaction Fleet coordination — this method itself never opens
    /// its own transaction so it can safely participate in an ambient one.
    /// requiredGarageUserId/requiredOwnerUserId, when non-null, are added to
    /// the WHERE clause — the ownership/garage-assignment guard lives in the
    /// same atomic statement as the status guard, never a separate check.
    /// Only the fields relevant to newStatus are actually written (see
    /// implementation) — a caller not needing a given field passes null.
    /// </summary>
    Task<bool> TryTransitionAsync(
        Guid requestId, IReadOnlyCollection<MaintenanceRequestStatus> allowedFromStatuses, MaintenanceRequestStatus newStatus,
        Guid? requiredGarageUserId, Guid? requiredOwnerUserId, decimal? estimatedCost, decimal? finalCost, string? reason,
        Guid? cancelledByUserId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryMarkSettledAsync(Guid requestId, DateTime utcNow, CancellationToken cancellationToken);
}
