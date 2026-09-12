using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideSafetyEventRepository : IRideSafetyEventRepository
{
    private readonly ApplicationDbContext _context;

    public RideSafetyEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RideSafetyEvent safetyEvent, CancellationToken cancellationToken)
    {
        await _context.RideSafetyEvents.AddAsync(safetyEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RideSafetyEvent>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        await _context.RideSafetyEvents.Where(safetyEvent => safetyEvent.RideId == rideId).ToListAsync(cancellationToken);

    public async Task<bool> TryAcknowledgeAsync(Guid safetyEventId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.RideSafetyEvents
            .Where(safetyEvent => safetyEvent.Id == safetyEventId && safetyEvent.Status == RideSafetyEventStatus.Open)
            .ExecuteUpdateAsync(setters => setters.SetProperty(safetyEvent => safetyEvent.Status, RideSafetyEventStatus.Acknowledged), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryResolveAsync(Guid safetyEventId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.RideSafetyEvents
            .Where(safetyEvent => safetyEvent.Id == safetyEventId && safetyEvent.Status != RideSafetyEventStatus.Resolved)
            .ExecuteUpdateAsync(setters => setters.SetProperty(safetyEvent => safetyEvent.Status, RideSafetyEventStatus.Resolved), cancellationToken);

        return rows == 1;
    }
}
