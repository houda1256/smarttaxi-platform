using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Notifications.Repositories;

internal sealed class ScheduledNotificationRepository : IScheduledNotificationRepository
{
    private readonly ApplicationDbContext _context;

    public ScheduledNotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(ScheduledNotification scheduled, CancellationToken cancellationToken)
    {
        await _context.ScheduledNotifications.AddAsync(scheduled, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(scheduled).State = EntityState.Detached;
            return false;
        }
    }

    public Task<ScheduledNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.ScheduledNotifications.FirstOrDefaultAsync(scheduled => scheduled.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ScheduledNotification>> GetDueAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        await _context.ScheduledNotifications
            .Where(scheduled => scheduled.Status == ScheduledNotificationStatus.Pending && scheduled.ScheduledAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryMarkProcessedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.ScheduledNotifications
            .Where(scheduled => scheduled.Id == id && scheduled.Status == ScheduledNotificationStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(scheduled => scheduled.Status, ScheduledNotificationStatus.Processed)
                .SetProperty(scheduled => scheduled.ProcessedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryMarkFailedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.ScheduledNotifications
            .Where(scheduled => scheduled.Id == id && scheduled.Status == ScheduledNotificationStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(scheduled => scheduled.Status, ScheduledNotificationStatus.Failed)
                .SetProperty(scheduled => scheduled.ProcessedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }
}
