using SmartTaxi.Application.Maintenance.Contracts;
using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Abstractions;

/// <summary>
/// Mandatory atomicity design (approved plan correction): ONE database
/// transaction contains the MaintenanceRequest -> Completed transition, the
/// immutable MaintenanceRecord insert, the authoritative FleetExpense insert
/// (via Fleet's own IFleetExpenseRepository — never a second expense system),
/// and a best-effort attempt to release the vehicle from UnderMaintenance back
/// to Active. The release attempt deliberately does NOT gate the transaction's
/// success: if the vehicle is no longer UnderMaintenance (e.g. independently
/// Suspended by an admin mid-repair), that is the correct, expected outcome —
/// "no reactivation required" — not a failure. The maintenance job itself
/// genuinely finished and must still be recorded/settled regardless of the
/// vehicle's independent state; forcing reactivation would be the actual bug.
/// A genuine DB exception during any step still rolls back everything.
/// </summary>
public interface IMaintenanceCompletionRepository
{
    Task<MaintenanceCompletionResult> TryCompleteAsync(
        Guid requestId, Guid garageUserId, Guid vehicleId, MaintenanceRecord record, FleetExpense expense, DateTime utcNow,
        CancellationToken cancellationToken);
}
