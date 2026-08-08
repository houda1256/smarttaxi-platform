using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideStatusHistoryRepository : IRideStatusHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public RideStatusHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<RideStatusHistory>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        await _context.RideStatusHistories
            .Where(history => history.RideId == rideId)
            .OrderBy(history => history.ChangedAt)
            .ToListAsync(cancellationToken);
}
