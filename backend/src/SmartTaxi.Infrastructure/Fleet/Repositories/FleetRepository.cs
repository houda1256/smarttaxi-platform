using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;
using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class FleetRepository : IFleetRepository
{
    private readonly ApplicationDbContext _context;

    public FleetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FleetOrganization fleet, CancellationToken cancellationToken)
    {
        await _context.Fleets.AddAsync(fleet, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<FleetOrganization?> GetByIdAsync(Guid fleetId, CancellationToken cancellationToken) =>
        _context.Fleets.FirstOrDefaultAsync(fleet => fleet.Id == fleetId, cancellationToken);

    public async Task<IReadOnlyCollection<FleetOrganization>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await _context.Fleets.Where(fleet => fleet.OwnerId == ownerId).ToListAsync(cancellationToken);

    public Task UpdateAsync(FleetOrganization fleet, CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySuspendAsync(Guid fleetId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Fleets
            .Where(fleet => fleet.Id == fleetId && fleet.Status == FleetStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(fleet => fleet.Status, FleetStatus.Suspended)
                .SetProperty(fleet => fleet.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
