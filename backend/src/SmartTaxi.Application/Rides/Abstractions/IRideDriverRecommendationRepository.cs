using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideDriverRecommendationRepository
{
    Task ReplaceForRideAsync(Guid rideId, IReadOnlyCollection<RideDriverRecommendation> recommendations, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RideDriverRecommendation>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken);
}
