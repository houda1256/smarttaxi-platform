using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeNotificationRealtimeNotifier : INotificationRealtimeNotifier
{
    public List<(Guid RecipientUserId, Guid NotificationId, string Category, string Title)> NewNotifications { get; } = [];

    public List<(Guid RecipientUserId, int UnreadCount)> UnreadCountChanges { get; } = [];

    public Task NotifyNewNotificationAsync(Guid recipientUserId, Guid notificationId, string category, string title, CancellationToken cancellationToken)
    {
        NewNotifications.Add((recipientUserId, notificationId, category, title));
        return Task.CompletedTask;
    }

    public Task NotifyUnreadCountChangedAsync(Guid recipientUserId, int unreadCount, CancellationToken cancellationToken)
    {
        UnreadCountChanges.Add((recipientUserId, unreadCount));
        return Task.CompletedTask;
    }
}
