using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideRatingRepository
{
    /// <summary>May throw a unique-violation-mapped Conflict — see the DB unique index on (RideId, ReviewerId).</summary>
    Task<bool> TryAddAsync(RideRating rating, CancellationToken cancellationToken);

    Task<bool> ExistsForReviewerAndRideAsync(Guid rideId, Guid reviewerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RideRating>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RideRating>> GetForReviewedUserAsync(Guid reviewedUserId, CancellationToken cancellationToken);
}
