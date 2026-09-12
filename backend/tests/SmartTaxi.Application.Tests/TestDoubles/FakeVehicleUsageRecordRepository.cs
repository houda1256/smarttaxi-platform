using SmartTaxi.Application.Fleet.UsageHistory.Abstractions;
using SmartTaxi.Domain.Fleet.UsageHistory.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeVehicleUsageRecordRepository : IVehicleUsageRecordRepository
{
    private readonly List<VehicleUsageRecord> _records = [];

    public IReadOnlyCollection<VehicleUsageRecord> Records => _records;

    public Task AddAsync(VehicleUsageRecord record, CancellationToken cancellationToken)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<VehicleUsageRecord>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<VehicleUsageRecord> records = _records.Where(r => r.VehicleId == vehicleId).ToList();
        return Task.FromResult(records);
    }

    public Task<IReadOnlyCollection<VehicleUsageRecord>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<VehicleUsageRecord> records = _records.Where(r => r.DriverId == driverId).ToList();
        return Task.FromResult(records);
    }
}
