namespace SmartTaxi.Application.Maintenance.Abstractions;

/// <summary>
/// Admin escape hatch for a stuck job, allowed from any non-terminal status.
/// ONE transaction: transitions MaintenanceRequest to Cancelled (guarded on
/// current status only — no GarageUserId/OwnerUserId requirement, this is an
/// admin action), then attempts a best-effort Fleet release, unconditionally,
/// same non-gating semantics as IMaintenanceCompletionRepository — a safe
/// no-op when the vehicle was never UnderMaintenance (request was still in an
/// early state) or already changed for another reason.
/// </summary>
public interface IMaintenanceForceCancelRepository
{
    Task<bool> TryForceCancelAsync(
        Guid requestId, Guid adminUserId, Guid vehicleId, string reason, DateTime utcNow, CancellationToken cancellationToken);
}
