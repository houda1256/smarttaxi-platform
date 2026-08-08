using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartTaxi.API.Realtime;

/// <summary>
/// Authenticated clients join a per-Ride or per-SharedRideMatch group to
/// receive live pushes; authorization that a given user may actually join a
/// specific Ride's group is left to the client only requesting groups for
/// rides it has legitimate access to — the Hub itself only requires a valid
/// JWT, mirroring the rest of the API's claims-based model.
/// </summary>
[Authorize]
public sealed class RideHub : Hub
{
    public Task JoinRideGroup(Guid rideId) => Groups.AddToGroupAsync(Context.ConnectionId, RideGroupName(rideId));

    public Task LeaveRideGroup(Guid rideId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, RideGroupName(rideId));

    public Task JoinSharedMatchGroup(Guid matchId) => Groups.AddToGroupAsync(Context.ConnectionId, SharedMatchGroupName(matchId));

    public Task LeaveSharedMatchGroup(Guid matchId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, SharedMatchGroupName(matchId));

    internal static string RideGroupName(Guid rideId) => $"ride-{rideId}";

    internal static string SharedMatchGroupName(Guid matchId) => $"shared-match-{matchId}";
}
