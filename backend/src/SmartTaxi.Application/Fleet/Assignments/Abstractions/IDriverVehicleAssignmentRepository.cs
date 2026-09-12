using SmartTaxi.Domain.Fleet.Assignments.Entities;

namespace SmartTaxi.Application.Fleet.Assignments.Abstractions;

public interface IDriverVehicleAssignmentRepository
{
    Task AddAsync(DriverVehicleAssignment assignment, CancellationToken cancellationToken);

    Task<DriverVehicleAssignment?> GetByIdAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DriverVehicleAssignment>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DriverVehicleAssignment>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    /// <summary>Non-terminal assignments (Draft/PendingApproval/Active) for a driver, used for overlap checks.</summary>
    Task<IReadOnlyCollection<DriverVehicleAssignment>> GetActiveOrPendingForDriverAsync(
        Guid driverId, Guid? excludeAssignmentId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DriverVehicleAssignment>> GetActiveOrPendingForVehicleAsync(
        Guid vehicleId, Guid? excludeAssignmentId, CancellationToken cancellationToken);

    Task<bool> TryApproveAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryActivateAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TrySuspendAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryCompleteAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryCancelAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken);
}
