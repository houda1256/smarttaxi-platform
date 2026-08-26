using SmartTaxi.Application.RoadsideAssistance.Contracts;

namespace SmartTaxi.Application.RoadsideAssistance.Abstractions;

/// <summary>
/// Mandatory atomicity design (approved plan, Q4): ONE transaction guards
/// RoadsideAssistanceRequest.EscalatedMaintenanceRequestId IS NULL AND
/// Status == Completed, creates the MaintenanceRequest via Module 9's own
/// public seam (MaintenanceRequest.Create + IMaintenanceRequestRepository.TryAddAsync
/// — never re-implemented, never a MaintenanceRecord write, never a garage
/// auto-selection), and sets EscalatedMaintenanceRequestId in the same
/// transaction. If Maintenance's own one-active-request-per-vehicle partial
/// unique index rejects the insert, the whole transaction rolls back and
/// EscalatedMaintenanceRequestId stays null — a retry is then safe and will
/// attempt the same insert again (idempotent by construction, not by a
/// separate dedup check).
/// </summary>
public interface IRoadsideEscalationRepository
{
    Task<RoadsideEscalationResult> TryEscalateAsync(
        Guid roadsideRequestId, Guid ownerUserId, Guid garageUserId, string description, DateTime utcNow, CancellationToken cancellationToken);
}
