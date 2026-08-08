using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideDriverRecommendationRepository : IRideDriverRecommendationRepository
{
    private readonly Dictionary<Guid, List<RideDriverRecommendation>> _byRideId = new();

    public Task ReplaceForRideAsync(
        Guid rideId, IReadOnlyCollection<RideDriverRecommendation> recommendations, CancellationToken cancellationToken)
    {
        _byRideId[rideId] = recommendations.ToList();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RideDriverRecommendation>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RideDriverRecommendation> recommendations = _byRideId.GetValueOrDefault(rideId, []);
        return Task.FromResult(recommendations);
    }
}
