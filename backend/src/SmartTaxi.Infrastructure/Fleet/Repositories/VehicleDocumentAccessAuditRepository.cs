using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class VehicleDocumentAccessAuditRepository : IVehicleDocumentAccessAuditRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleDocumentAccessAuditRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(VehicleDocumentAccessAuditEntry entry, CancellationToken cancellationToken)
    {
        await _context.VehicleDocumentAccessAuditEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
