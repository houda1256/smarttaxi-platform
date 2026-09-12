using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeScheduledNotificationRepository : IScheduledNotificationRepository
{
    private readonly Dictionary<Guid, ScheduledNotification> _scheduled = new();

    public Task<bool> TryAddAsync(ScheduledNotification scheduled, CancellationToken cancellationToken)
    {
        if (_scheduled.Values.Any(s => s.IdempotencyKey == scheduled.IdempotencyKey))
        {
            return Task.FromResult(false);
        }

        _scheduled[scheduled.Id] = scheduled;
        return Task.FromResult(true);
    }

    public Task<ScheduledNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_scheduled.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<ScheduledNotification>> GetDueAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<ScheduledNotification>>(
            _scheduled.Values.Where(s => s.Status == ScheduledNotificationStatus.Pending && s.ScheduledAtUtc <= utcNow).ToList());

    public Task<bool> TryMarkProcessedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        var scheduled = _scheduled.GetValueOrDefault(id);

        if (scheduled is null || scheduled.Status != ScheduledNotificationStatus.Pending)
        {
            return Task.FromResult(false);
        }

        scheduled.MarkProcessed(utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryMarkFailedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        var scheduled = _scheduled.GetValueOrDefault(id);

        if (scheduled is null || scheduled.Status != ScheduledNotificationStatus.Pending)
        {
            return Task.FromResult(false);
        }

        scheduled.MarkFailed(utcNow);
        return Task.FromResult(true);
    }
}
