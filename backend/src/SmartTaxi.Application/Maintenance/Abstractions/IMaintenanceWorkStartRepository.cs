using SmartTaxi.Application.Maintenance.Contracts;

namespace SmartTaxi.Application.Maintenance.Abstractions;

/// <summary>
/// Mandatory atomicity design (approved plan correction): ONE database
/// transaction guarantees MaintenanceRequest.Status == InProgress AND
/// Vehicle.OperationalStatus == UnderMaintenance after commit, or neither
/// changed — never two independently committed operations. No compensation
/// logic is the primary design; a crash mid-transaction rolls back cleanly by
/// construction (standard DB transaction semantics), so a vehicle can never
/// be left UnderMaintenance with no corresponding InProgress job.
/// </summary>
public interface IMaintenanceWorkStartRepository
{
    Task<MaintenanceWorkStartResult> TryStartAsync(
        Guid requestId, Guid garageUserId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);
}
