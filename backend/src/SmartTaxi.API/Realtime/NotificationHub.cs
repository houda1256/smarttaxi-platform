using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartTaxi.API.Realtime;

/// <summary>
/// Unlike RideHub (whose per-Ride group is joined on client request, with
/// authorization left to the client only requesting rides it has legitimate
/// access to — see RideHub's own doc comment), this Hub exposes no
/// client-callable "join" method at all. Group membership is derived only from
/// the connection's own authenticated JWT subject claim in OnConnectedAsync,
/// so a client can never request or guess its way into another user's group —
/// there is no input for it to supply.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));
        }

        await base.OnConnectedAsync();
    }

    internal static string UserGroupName(string userId) => $"user-{userId}";
}
