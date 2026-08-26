using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Maintenance.Repositories;

internal sealed class MaintenanceRecordRepository : IMaintenanceRecordRepository
{
    private readonly ApplicationDbContext _context;

    public MaintenanceRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<MaintenanceRecord?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken) =>
        _context.MaintenanceRecords.FirstOrDefaultAsync(record => record.Id == recordId, cancellationToken);

    public async Task<IReadOnlyCollection<MaintenanceRecord>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        await _context.MaintenanceRecords.Where(record => record.VehicleId == vehicleId)
            .OrderByDescending(record => record.InterventionDate).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<MaintenanceRecord>> GetDueForReminderAsync(DateOnly today, CancellationToken cancellationToken) =>
        await _context.MaintenanceRecords
            .Where(record => record.NextRecommendedServiceDate != null && record.NextRecommendedServiceDate <= today)
            .ToListAsync(cancellationToken);
}
