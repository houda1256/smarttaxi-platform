using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.UsageHistory.Abstractions;
using SmartTaxi.Domain.Fleet.UsageHistory.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class VehicleUsageRecordRepository : IVehicleUsageRecordRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleUsageRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(VehicleUsageRecord record, CancellationToken cancellationToken)
    {
        await _context.VehicleUsageRecords.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<VehicleUsageRecord>> GetForVehicleAsync(
        Guid vehicleId, CancellationToken cancellationToken) =>
        await _context.VehicleUsageRecords.Where(record => record.VehicleId == vehicleId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<VehicleUsageRecord>> GetForDriverAsync(
        Guid driverId, CancellationToken cancellationToken) =>
        await _context.VehicleUsageRecords.Where(record => record.DriverId == driverId).ToListAsync(cancellationToken);
}
