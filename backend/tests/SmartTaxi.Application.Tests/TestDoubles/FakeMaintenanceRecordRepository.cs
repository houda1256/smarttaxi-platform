using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeMaintenanceRecordRepository : IMaintenanceRecordRepository
{
    private readonly Dictionary<Guid, MaintenanceRecord> _records = new();

    public void Add(MaintenanceRecord record) => _records[record.Id] = record;

    public Task<MaintenanceRecord?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken) =>
        Task.FromResult(_records.GetValueOrDefault(recordId));

    public Task<IReadOnlyCollection<MaintenanceRecord>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<MaintenanceRecord> items = _records.Values.Where(r => r.VehicleId == vehicleId).ToList();
        return Task.FromResult(items);
    }

    public Task<IReadOnlyCollection<MaintenanceRecord>> GetDueForReminderAsync(DateOnly today, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<MaintenanceRecord> items = _records.Values
            .Where(r => r.NextRecommendedServiceDate is not null && r.NextRecommendedServiceDate <= today).ToList();
        return Task.FromResult(items);
    }
}
