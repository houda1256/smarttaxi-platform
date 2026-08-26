using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Abstractions;

public interface IScheduledNotificationRepository
{
    /// <summary>False on an idempotency-key collision — same guarantee as INotificationRepository.TryAddAsync.</summary>
    Task<bool> TryAddAsync(ScheduledNotification scheduled, CancellationToken cancellationToken);

    Task<ScheduledNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ScheduledNotification>> GetDueAsync(DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryMarkProcessedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryMarkFailedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken);
}
