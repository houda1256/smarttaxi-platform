using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideDriverRecommendationRepository : IRideDriverRecommendationRepository
{
    private readonly ApplicationDbContext _context;

    public RideDriverRecommendationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ReplaceForRideAsync(
        Guid rideId, IReadOnlyCollection<RideDriverRecommendation> recommendations, CancellationToken cancellationToken)
    {
        await _context.RideDriverRecommendations
            .Where(recommendation => recommendation.RideId == rideId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.RideDriverRecommendations.AddRangeAsync(recommendations, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RideDriverRecommendation>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        await _context.RideDriverRecommendations
            .Where(recommendation => recommendation.RideId == rideId)
            .OrderBy(recommendation => recommendation.Rank)
            .ToListAsync(cancellationToken);
}
