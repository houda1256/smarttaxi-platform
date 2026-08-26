using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Notifications.Repositories;

/// <summary>TryAddAsync catches the IdempotencyKey unique index violation — same "atomic guard at the DB level" convention as SubscriptionRepository.TryAddAsync.</summary>
internal sealed class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(Notification notification, CancellationToken cancellationToken)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(notification).State = EntityState.Detached;
            return false;
        }
    }

    public Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken) =>
        _context.Notifications.FirstOrDefaultAsync(notification => notification.Id == notificationId, cancellationToken);

    public Task<Notification?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        _context.Notifications.FirstOrDefaultAsync(notification => notification.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<PagedResult<Notification>> GetForRecipientAsync(
        Guid recipientUserId, bool unreadOnly, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Notifications.Where(notification => notification.RecipientUserId == recipientUserId);

        if (unreadOnly)
        {
            query = query.Where(notification => notification.ReadAtUtc == null);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Notification>(items, totalCount, pageNumber, pageSize);
    }

    public Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken cancellationToken) =>
        _context.Notifications.CountAsync(
            notification => notification.RecipientUserId == recipientUserId && notification.ReadAtUtc == null, cancellationToken);

    public async Task<bool> TryMarkAsReadAsync(Guid notificationId, Guid recipientUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Notifications
            .Where(notification => notification.Id == notificationId && notification.RecipientUserId == recipientUserId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.ReadAtUtc, notification => notification.ReadAtUtc ?? utcNow), cancellationToken);

        return rows == 1;
    }

    public Task<int> MarkAllAsReadAsync(Guid recipientUserId, DateTime utcNow, CancellationToken cancellationToken) =>
        _context.Notifications
            .Where(notification => notification.RecipientUserId == recipientUserId && notification.ReadAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.ReadAtUtc, utcNow), cancellationToken);
}
