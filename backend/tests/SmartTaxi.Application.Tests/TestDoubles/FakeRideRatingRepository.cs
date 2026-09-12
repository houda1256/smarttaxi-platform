using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideRatingRepository : IRideRatingRepository
{
    private readonly List<RideRating> _ratings = [];

    public Task<bool> TryAddAsync(RideRating rating, CancellationToken cancellationToken)
    {
        if (_ratings.Any(r => r.RideId == rating.RideId && r.ReviewerId == rating.ReviewerId))
        {
            return Task.FromResult(false);
        }

        _ratings.Add(rating);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsForReviewerAndRideAsync(Guid rideId, Guid reviewerId, CancellationToken cancellationToken) =>
        Task.FromResult(_ratings.Any(r => r.RideId == rideId && r.ReviewerId == reviewerId));

    public Task<IReadOnlyCollection<RideRating>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RideRating> ratings = _ratings.Where(r => r.RideId == rideId).ToList();
        return Task.FromResult(ratings);
    }

    public Task<IReadOnlyCollection<RideRating>> GetForReviewedUserAsync(Guid reviewedUserId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RideRating> ratings = _ratings.Where(r => r.ReviewedUserId == reviewedUserId).ToList();
        return Task.FromResult(ratings);
    }
}
