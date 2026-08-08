using Microsoft.AspNetCore.SignalR;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.API.Realtime;

/// <summary>The real, Hub-backed implementation of the Application-layer DIP boundary — kept in the API project so Application never references Microsoft.AspNetCore.SignalR.</summary>
internal sealed class SignalRRideRealtimeNotifier : IRideRealtimeNotifier
{
    private readonly IHubContext<RideHub> _hubContext;

    public SignalRRideRealtimeNotifier(IHubContext<RideHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyRideStatusChangedAsync(Guid rideId, string newStatus, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(RideHub.RideGroupName(rideId)).SendAsync("RideStatusChanged", rideId, newStatus, cancellationToken);

    public Task NotifyLocationUpdatedAsync(Guid rideId, double latitude, double longitude, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(RideHub.RideGroupName(rideId)).SendAsync("RideLocationUpdated", rideId, latitude, longitude, cancellationToken);

    public Task NotifySharedRideUpdatedAsync(Guid matchId, string newStatus, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(RideHub.SharedMatchGroupName(matchId)).SendAsync("SharedRideUpdated", matchId, newStatus, cancellationToken);
}
