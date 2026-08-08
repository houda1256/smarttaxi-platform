namespace SmartTaxi.Application.Rides.Abstractions;

/// <summary>
/// Dependency-inversion boundary so Application never references
/// Microsoft.AspNetCore.SignalR directly — the real implementation (backed
/// by a Hub) lives in the API project; a no-op implementation is used in
/// tests.
/// </summary>
public interface IRideRealtimeNotifier
{
    Task NotifyRideStatusChangedAsync(Guid rideId, string newStatus, CancellationToken cancellationToken);

    Task NotifyLocationUpdatedAsync(Guid rideId, double latitude, double longitude, CancellationToken cancellationToken);

    Task NotifySharedRideUpdatedAsync(Guid matchId, string newStatus, CancellationToken cancellationToken);
}
