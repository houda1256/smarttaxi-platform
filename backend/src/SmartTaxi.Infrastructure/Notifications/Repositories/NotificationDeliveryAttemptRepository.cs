using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Notifications.Repositories;

internal sealed class NotificationDeliveryAttemptRepository : INotificationDeliveryAttemptRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationDeliveryAttemptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken)
    {
        await _context.NotificationDeliveryAttempts.AddAsync(attempt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken)
    {
        _context.NotificationDeliveryAttempts.Update(attempt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<NotificationDeliveryAttempt?> GetByIdAsync(Guid attemptId, CancellationToken cancellationToken) =>
        _context.NotificationDeliveryAttempts.FirstOrDefaultAsync(attempt => attempt.Id == attemptId, cancellationToken);

    public async Task<IReadOnlyCollection<NotificationDeliveryAttempt>> GetForNotificationAsync(
        Guid notificationId, CancellationToken cancellationToken) =>
        await _context.NotificationDeliveryAttempts
            .Where(attempt => attempt.NotificationId == notificationId)
            .OrderBy(attempt => attempt.AttemptedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<NotificationDeliveryAttempt>> GetRetryableAsync(
        DateTime utcNow, int maxAttempts, CancellationToken cancellationToken) =>
        await _context.NotificationDeliveryAttempts
            .Where(attempt => attempt.Status == NotificationDeliveryStatus.Failed
                && attempt.NextAttemptAtUtc != null && attempt.NextAttemptAtUtc <= utcNow
                && attempt.AttemptNumber < maxAttempts)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<NotificationDeliveryAttempt>> GetFailedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.NotificationDeliveryAttempts.Where(attempt => attempt.Status == NotificationDeliveryStatus.Failed);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(attempt => attempt.AttemptedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDeliveryAttempt>(items, totalCount, pageNumber, pageSize);
    }
}
