using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideLocationPointRepository
{
    Task AddAsync(RideLocationPoint point, CancellationToken cancellationToken);

    Task<RideLocationPoint?> GetLatestForRideAsync(Guid rideId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RideLocationPoint>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken);

    /// <summary>Deletes points recorded before the cutoff — the retention-policy enforcement point (no background job; called on demand).</summary>
    Task<int> PruneOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken);
}
