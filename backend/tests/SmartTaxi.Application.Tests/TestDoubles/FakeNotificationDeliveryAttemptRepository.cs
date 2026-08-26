using SmartTaxi.Application.Common;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeNotificationDeliveryAttemptRepository : INotificationDeliveryAttemptRepository
{
    private readonly Dictionary<Guid, NotificationDeliveryAttempt> _attempts = new();

    public Task AddAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken)
    {
        _attempts[attempt.Id] = attempt;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken)
    {
        _attempts[attempt.Id] = attempt;
        return Task.CompletedTask;
    }

    public Task<NotificationDeliveryAttempt?> GetByIdAsync(Guid attemptId, CancellationToken cancellationToken) =>
        Task.FromResult(_attempts.GetValueOrDefault(attemptId));

    public Task<IReadOnlyCollection<NotificationDeliveryAttempt>> GetForNotificationAsync(Guid notificationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<NotificationDeliveryAttempt>>(
            _attempts.Values.Where(a => a.NotificationId == notificationId).ToList());

    public Task<IReadOnlyCollection<NotificationDeliveryAttempt>> GetRetryableAsync(
        DateTime utcNow, int maxAttempts, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<NotificationDeliveryAttempt>>(
            _attempts.Values.Where(a => a.Status == NotificationDeliveryStatus.Failed && a.NextAttemptAtUtc is not null
                && a.NextAttemptAtUtc <= utcNow && a.AttemptNumber < maxAttempts).ToList());

    public Task<PagedResult<NotificationDeliveryAttempt>> GetFailedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var failed = _attempts.Values.Where(a => a.Status == NotificationDeliveryStatus.Failed)
            .OrderByDescending(a => a.AttemptedAtUtc).ToList();
        var page = failed.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<NotificationDeliveryAttempt>(page, failed.Count, pageNumber, pageSize));
    }
}
