using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideRealtimeNotifier : IRideRealtimeNotifier
{
    public List<(Guid RideId, string NewStatus)> StatusNotifications { get; } = [];
    public List<(Guid RideId, double Latitude, double Longitude)> LocationNotifications { get; } = [];
    public List<(Guid MatchId, string NewStatus)> SharedRideNotifications { get; } = [];

    public Task NotifyRideStatusChangedAsync(Guid rideId, string newStatus, CancellationToken cancellationToken)
    {
        StatusNotifications.Add((rideId, newStatus));
        return Task.CompletedTask;
    }

    public Task NotifyLocationUpdatedAsync(Guid rideId, double latitude, double longitude, CancellationToken cancellationToken)
    {
        LocationNotifications.Add((rideId, latitude, longitude));
        return Task.CompletedTask;
    }

    public Task NotifySharedRideUpdatedAsync(Guid matchId, string newStatus, CancellationToken cancellationToken)
    {
        SharedRideNotifications.Add((matchId, newStatus));
        return Task.CompletedTask;
    }
}
