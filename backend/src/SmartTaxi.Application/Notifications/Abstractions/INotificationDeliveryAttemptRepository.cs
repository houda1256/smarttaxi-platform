using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Abstractions;

public interface INotificationDeliveryAttemptRepository
{
    Task AddAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken);

    Task UpdateAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken);

    Task<NotificationDeliveryAttempt?> GetByIdAsync(Guid attemptId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<NotificationDeliveryAttempt>> GetForNotificationAsync(Guid notificationId, CancellationToken cancellationToken);

    /// <summary>Failed attempts whose NextAttemptAtUtc is due and whose AttemptNumber has not yet reached the configured cap.</summary>
    Task<IReadOnlyCollection<NotificationDeliveryAttempt>> GetRetryableAsync(DateTime utcNow, int maxAttempts, CancellationToken cancellationToken);

    Task<PagedResult<NotificationDeliveryAttempt>> GetFailedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}
