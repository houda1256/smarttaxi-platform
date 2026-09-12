using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideLocationPointRepository : IRideLocationPointRepository
{
    private readonly List<RideLocationPoint> _points = [];

    public Task AddAsync(RideLocationPoint point, CancellationToken cancellationToken)
    {
        _points.Add(point);
        return Task.CompletedTask;
    }

    public Task<RideLocationPoint?> GetLatestForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        var latest = _points.Where(p => p.RideId == rideId).OrderByDescending(p => p.RecordedAt).FirstOrDefault();
        return Task.FromResult(latest);
    }

    public Task<IReadOnlyCollection<RideLocationPoint>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RideLocationPoint> points = _points.Where(p => p.RideId == rideId).ToList();
        return Task.FromResult(points);
    }

    public Task<int> PruneOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        var removed = _points.RemoveAll(p => p.RecordedAt < cutoffUtc);
        return Task.FromResult(removed);
    }
}
