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
}
