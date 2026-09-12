using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideLocationPointRepository : IRideLocationPointRepository
{
    private readonly ApplicationDbContext _context;

    public RideLocationPointRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RideLocationPoint point, CancellationToken cancellationToken)
    {
        await _context.RideLocationPoints.AddAsync(point, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RideLocationPoint?> GetLatestForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        _context.RideLocationPoints
            .Where(point => point.RideId == rideId)
            .OrderByDescending(point => point.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<RideLocationPoint>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        await _context.RideLocationPoints
            .Where(point => point.RideId == rideId)
            .OrderBy(point => point.RecordedAt)
            .ToListAsync(cancellationToken);

    public async Task<int> PruneOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken) =>
        await _context.RideLocationPoints
            .Where(point => point.RecordedAt < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
}
