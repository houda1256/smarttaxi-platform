namespace SmartTaxi.Application.Notifications.Abstractions;

/// <summary>
/// Dependency-inversion boundary so Application never references
/// Microsoft.AspNetCore.SignalR directly — mirrors Rides' IRideRealtimeNotifier.
/// Unlike RideHub (resource-group membership left to client trust), the real
/// implementation's Hub places a connection into a group derived only from the
/// authenticated JWT's own subject claim (see NotificationHub) — a client can
/// never join another user's group. A failure here must never lose the
/// notification: it is only ever called after the Notification row and its
/// InApp NotificationDeliveryAttempt are already persisted.
/// </summary>
public interface INotificationRealtimeNotifier
{
    Task NotifyNewNotificationAsync(Guid recipientUserId, Guid notificationId, string category, string title, CancellationToken cancellationToken);

    Task NotifyUnreadCountChangedAsync(Guid recipientUserId, int unreadCount, CancellationToken cancellationToken);
}
