using SmartTaxi.Application.Common;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeNotificationRepository : INotificationRepository
{
    private readonly Dictionary<Guid, Notification> _notifications = new();

    public Task<bool> TryAddAsync(Notification notification, CancellationToken cancellationToken)
    {
        if (_notifications.Values.Any(n => n.IdempotencyKey == notification.IdempotencyKey))
        {
            return Task.FromResult(false);
        }

        _notifications[notification.Id] = notification;
        return Task.FromResult(true);
    }

    public Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken) =>
        Task.FromResult(_notifications.GetValueOrDefault(notificationId));

    public Task<Notification?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        Task.FromResult(_notifications.Values.FirstOrDefault(n => n.IdempotencyKey == idempotencyKey));

    public Task<PagedResult<Notification>> GetForRecipientAsync(
        Guid recipientUserId, bool unreadOnly, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _notifications.Values.Where(n => n.RecipientUserId == recipientUserId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var ordered = query.OrderByDescending(n => n.CreatedAtUtc).ToList();
        var page = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<Notification>(page, ordered.Count, pageNumber, pageSize));
    }

    public Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken cancellationToken) =>
        Task.FromResult(_notifications.Values.Count(n => n.RecipientUserId == recipientUserId && !n.IsRead));

    public Task<bool> TryMarkAsReadAsync(Guid notificationId, Guid recipientUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var notification = _notifications.GetValueOrDefault(notificationId);

        if (notification is null || notification.RecipientUserId != recipientUserId)
        {
            return Task.FromResult(false);
        }

        notification.MarkAsRead(utcNow);
        return Task.FromResult(true);
    }

    public Task<int> MarkAllAsReadAsync(Guid recipientUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var unread = _notifications.Values.Where(n => n.RecipientUserId == recipientUserId && !n.IsRead).ToList();

        foreach (var notification in unread)
        {
            notification.MarkAsRead(utcNow);
        }

        return Task.FromResult(unread.Count);
    }
}
