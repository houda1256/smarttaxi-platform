using Microsoft.AspNetCore.SignalR;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.API.Realtime;

/// <summary>The real, Hub-backed implementation of the Application-layer DIP boundary — kept in the API project so Application never references Microsoft.AspNetCore.SignalR, mirroring SignalRRideRealtimeNotifier.</summary>
internal sealed class SignalRNotificationRealtimeNotifier : INotificationRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationRealtimeNotifier(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyNewNotificationAsync(Guid recipientUserId, Guid notificationId, string category, string title, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(NotificationHub.UserGroupName(recipientUserId.ToString()))
            .SendAsync("NewNotification", notificationId, category, title, cancellationToken);

    public Task NotifyUnreadCountChangedAsync(Guid recipientUserId, int unreadCount, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group(NotificationHub.UserGroupName(recipientUserId.ToString()))
            .SendAsync("UnreadCountChanged", unreadCount, cancellationToken);
}
