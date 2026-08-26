using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Abstractions;

/// <summary>No Update/Delete method exists here by design — MaintenanceRecord is the immutable digital maintenance book, inserted exactly once by IMaintenanceCompletionRepository. Enforcing immutability by omission, not by discipline.</summary>
public interface IMaintenanceRecordRepository
{
    Task<MaintenanceRecord?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MaintenanceRecord>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    /// <summary>Every record whose NextRecommendedServiceDate has passed — the date-based preventive-maintenance sweep's input (§L, MVP). Does not deduplicate "latest per vehicle": each due record fires its own notification once (Notifications' own SourceType/SourceId idempotency prevents re-dispatch across repeated sweeps).</summary>
    Task<IReadOnlyCollection<MaintenanceRecord>> GetDueForReminderAsync(DateOnly today, CancellationToken cancellationToken);
}
