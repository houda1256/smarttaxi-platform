using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Abstractions;

public interface INotificationRepository
{
    /// <summary>False when the (SourceType, SourceId, RecipientUserId, Category) unique index rejects a concurrent duplicate — the idempotency guarantee.</summary>
    Task<bool> TryAddAsync(Notification notification, CancellationToken cancellationToken);

    Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken);

    Task<Notification?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<PagedResult<Notification>> GetForRecipientAsync(
        Guid recipientUserId, bool unreadOnly, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken cancellationToken);

    /// <summary>Atomic and ownership-scoped in the same query (defense in depth on top of the handler's own ownership check) — idempotent, never fails on an already-read row.</summary>
    Task<bool> TryMarkAsReadAsync(Guid notificationId, Guid recipientUserId, DateTime utcNow, CancellationToken cancellationToken);

    Task<int> MarkAllAsReadAsync(Guid recipientUserId, DateTime utcNow, CancellationToken cancellationToken);
}
