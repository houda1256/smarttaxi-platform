using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Rides.Repositories;

internal sealed class RideRatingRepository : IRideRatingRepository
{
    private readonly ApplicationDbContext _context;

    public RideRatingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(RideRating rating, CancellationToken cancellationToken)
    {
        await _context.RideRatings.AddAsync(rating, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The unique index on (RideId, ReviewerId) rejected a concurrent duplicate rating.
            _context.Entry(rating).State = EntityState.Detached;
            return false;
        }
    }

    public Task<bool> ExistsForReviewerAndRideAsync(Guid rideId, Guid reviewerId, CancellationToken cancellationToken) =>
        _context.RideRatings.AnyAsync(rating => rating.RideId == rideId && rating.ReviewerId == reviewerId, cancellationToken);

    public async Task<IReadOnlyCollection<RideRating>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken) =>
        await _context.RideRatings.Where(rating => rating.RideId == rideId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<RideRating>> GetForReviewedUserAsync(Guid reviewedUserId, CancellationToken cancellationToken) =>
        await _context.RideRatings.Where(rating => rating.ReviewedUserId == reviewedUserId).ToListAsync(cancellationToken);
}
