using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Domain.Subscriptions.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Subscriptions.Repositories;

/// <summary>
/// PendingOrActiveStatuses mirrors the DB partial unique index's own filter
/// (Status IN ('Pending','Active')) so "does a live subscription already
/// exist" queries stay translatable to SQL — same duplication pattern
/// PaymentRepository uses for its own terminal-status list.
/// </summary>
internal sealed class SubscriptionRepository : ISubscriptionRepository
{
    private static readonly SubscriptionStatus[] PendingOrActiveStatuses = [SubscriptionStatus.Pending, SubscriptionStatus.Active];
    private static readonly SubscriptionStatus[] CancellableStatuses =
        [SubscriptionStatus.Pending, SubscriptionStatus.Active, SubscriptionStatus.Suspended];
    private static readonly SubscriptionStatus[] ExpirableStatuses = [SubscriptionStatus.Active, SubscriptionStatus.Suspended];

    private readonly ApplicationDbContext _context;

    public SubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The partial unique index on (SubscriberId, TargetRole) rejected a concurrent duplicate subscription.
            _context.Entry(subscription).State = EntityState.Detached;
            return false;
        }
    }

    public Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken) =>
        _context.Subscriptions.FirstOrDefaultAsync(subscription => subscription.Id == subscriptionId, cancellationToken);

    public Task<Subscription?> GetActiveOrPendingForSubscriberAsync(
        Guid subscriberId, UserRole targetRole, CancellationToken cancellationToken) =>
        _context.Subscriptions.FirstOrDefaultAsync(
            subscription => subscription.SubscriberId == subscriberId && subscription.TargetRole == targetRole
                && PendingOrActiveStatuses.Contains(subscription.Status),
            cancellationToken);

    public async Task<IReadOnlyCollection<Subscription>> GetHistoryForSubscriberAsync(
        Guid subscriberId, CancellationToken cancellationToken) =>
        await _context.Subscriptions
            .Where(subscription => subscription.SubscriberId == subscriberId)
            .OrderByDescending(subscription => subscription.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Subscription>> GetActiveExpiredAsOfAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        await _context.Subscriptions
            .Where(subscription => subscription.Status == SubscriptionStatus.Active && subscription.EndDate <= utcNow)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryActivateAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Subscriptions
            .Where(subscription => subscription.Id == subscriptionId && subscription.Status == SubscriptionStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(subscription => subscription.Status, SubscriptionStatus.Active)
                .SetProperty(subscription => subscription.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryRenewAsync(
        Guid subscriptionId, DateTime expectedCurrentEndDate, DateTime newEndDate, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Subscriptions
            .Where(subscription => subscription.Id == subscriptionId && subscription.Status == SubscriptionStatus.Active
                && subscription.EndDate == expectedCurrentEndDate)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(subscription => subscription.EndDate, newEndDate)
                .SetProperty(subscription => subscription.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TrySuspendAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Subscriptions
            .Where(subscription => subscription.Id == subscriptionId && subscription.Status == SubscriptionStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(subscription => subscription.Status, SubscriptionStatus.Suspended)
                .SetProperty(subscription => subscription.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryCancelAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Subscriptions
            .Where(subscription => subscription.Id == subscriptionId && CancellableStatuses.Contains(subscription.Status))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(subscription => subscription.Status, SubscriptionStatus.Cancelled)
                .SetProperty(subscription => subscription.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryExpireAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Subscriptions
            .Where(subscription => subscription.Id == subscriptionId && ExpirableStatuses.Contains(subscription.Status))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(subscription => subscription.Status, SubscriptionStatus.Expired)
                .SetProperty(subscription => subscription.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryChangePlanAsync(
        Guid subscriptionId, Guid newPlanId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Subscriptions
            .Where(subscription => subscription.Id == subscriptionId && subscription.Status == SubscriptionStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(subscription => subscription.PlanId, newPlanId)
                .SetProperty(subscription => subscription.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
