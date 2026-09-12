using SmartTaxi.Domain.Fleet.UsageHistory.Entities;

namespace SmartTaxi.Application.Fleet.UsageHistory.Abstractions;

public interface IVehicleUsageRecordRepository
{
    Task AddAsync(VehicleUsageRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VehicleUsageRecord>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VehicleUsageRecord>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken);
}
