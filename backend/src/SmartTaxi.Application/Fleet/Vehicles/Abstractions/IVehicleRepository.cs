using SmartTaxi.Domain.Fleet.Vehicles.Entities;

namespace SmartTaxi.Application.Fleet.Vehicles.Abstractions;

public interface IVehicleRepository
{
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);

    Task<Vehicle?> GetByIdAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Vehicle>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Vehicle>> GetForFleetAsync(Guid fleetId, CancellationToken cancellationToken);

    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken);

    Task<bool> ExistsWithLicensePlateAsync(string licensePlate, Guid? excludeVehicleId, CancellationToken cancellationToken);

    Task<bool> ExistsWithVinAsync(string vin, Guid? excludeVehicleId, CancellationToken cancellationToken);

    Task<bool> TryApproveAsync(Guid vehicleId, Guid approvedBy, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(Guid vehicleId, Guid rejectedBy, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TrySuspendAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryRetireAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Module 9 (Maintenance &amp; Garage) integration seam — only succeeds WHERE
    /// OperationalStatus == Active, so a vehicle that's already Suspended/
    /// Unavailable/etc. cannot enter maintenance this way. Intended to be
    /// called from within Maintenance's own atomic cross-aggregate
    /// transaction (see IMaintenanceWorkStartRepository), never independently.
    /// </summary>
    Task<bool> TryMarkUnderMaintenanceAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Module 9 integration seam — only succeeds WHERE OperationalStatus ==
    /// UnderMaintenance, which is exactly what prevents accidentally
    /// reactivating a vehicle that became Suspended/Unavailable for an
    /// unrelated reason while maintenance was in progress: if the status has
    /// since changed, this call safely no-ops (returns false) instead of
    /// overwriting it. Intended to be called from within Maintenance's own
    /// atomic cross-aggregate transactions (IMaintenanceCompletionRepository/
    /// IMaintenanceForceCancelRepository), never independently.
    /// </summary>
    Task<bool> TryReleaseFromMaintenanceAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Module 10 (Roadside Assistance) integration seam — only succeeds WHERE
    /// OperationalStatus == Active, so a vehicle that's already Suspended/
    /// UnderMaintenance/etc. cannot enter roadside assistance this way. Used
    /// only for immobilizing RoadsideServiceType values (Towing/
    /// MechanicalBreakdownAssistance/AccidentAssistance) — intended to be
    /// called from within Roadside Assistance's own atomic cross-aggregate
    /// transaction (see IRoadsideWorkStartRepository), never independently.
    /// A distinct status from UnderMaintenance by design — the two modules
    /// never share the same OperationalStatus value.
    /// </summary>
    Task<bool> TryMarkUnderRoadsideAssistanceAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Module 10 integration seam — only succeeds WHERE OperationalStatus ==
    /// UnderRoadsideAssistance, which is exactly what prevents accidentally
    /// reactivating a vehicle that became Suspended/UnderMaintenance/etc. for
    /// an unrelated reason while the intervention was in progress: if the
    /// status has since changed, this call safely no-ops (returns false)
    /// instead of overwriting it. Intended to be called from within Roadside
    /// Assistance's own atomic cross-aggregate transactions
    /// (IRoadsideCompletionRepository/IRoadsideForceCancelRepository), never
    /// independently.
    /// </summary>
    Task<bool> TryReleaseFromRoadsideAssistanceAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);
}
