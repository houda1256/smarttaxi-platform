using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSubscriptionRepository : ISubscriptionRepository
{
    private static readonly SubscriptionStatus[] PendingOrActiveStatuses = [SubscriptionStatus.Pending, SubscriptionStatus.Active];
    private static readonly SubscriptionStatus[] CancellableStatuses =
        [SubscriptionStatus.Pending, SubscriptionStatus.Active, SubscriptionStatus.Suspended];
    private static readonly SubscriptionStatus[] ExpirableStatuses = [SubscriptionStatus.Active, SubscriptionStatus.Suspended];

    private readonly Dictionary<Guid, Subscription> _subscriptionsById = new();

    public Task<bool> TryAddAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        var conflict = _subscriptionsById.Values.Any(s =>
            s.SubscriberId == subscription.SubscriberId && s.TargetRole == subscription.TargetRole
            && PendingOrActiveStatuses.Contains(s.Status));

        if (conflict)
        {
            return Task.FromResult(false);
        }

        _subscriptionsById[subscription.Id] = subscription;
        return Task.FromResult(true);
    }

    public Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken) =>
        Task.FromResult(_subscriptionsById.GetValueOrDefault(subscriptionId));

    public Task<Subscription?> GetActiveOrPendingForSubscriberAsync(Guid subscriberId, UserRole targetRole, CancellationToken cancellationToken) =>
        Task.FromResult(_subscriptionsById.Values.FirstOrDefault(s =>
            s.SubscriberId == subscriberId && s.TargetRole == targetRole && PendingOrActiveStatuses.Contains(s.Status)));

    public Task<IReadOnlyCollection<Subscription>> GetHistoryForSubscriberAsync(Guid subscriberId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Subscription>>(
            _subscriptionsById.Values.Where(s => s.SubscriberId == subscriberId).OrderByDescending(s => s.CreatedAt).ToList());

    public Task<bool> TryActivateAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(subscriptionId, s => s.Status == SubscriptionStatus.Pending, SubscriptionStatus.Active);

    public Task<bool> TryRenewAsync(
        Guid subscriptionId, DateTime expectedCurrentEndDate, DateTime newEndDate, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_subscriptionsById.TryGetValue(subscriptionId, out var subscription)
            || subscription.Status != SubscriptionStatus.Active
            || subscription.EndDate != expectedCurrentEndDate)
        {
            return Task.FromResult(false);
        }

        SetProperty(subscription, nameof(Subscription.EndDate), newEndDate);
        return Task.FromResult(true);
    }

    public Task<bool> TrySuspendAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(subscriptionId, s => s.Status == SubscriptionStatus.Active, SubscriptionStatus.Suspended);

    public Task<bool> TryCancelAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(subscriptionId, s => CancellableStatuses.Contains(s.Status), SubscriptionStatus.Cancelled);

    public Task<bool> TryExpireAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(subscriptionId, s => ExpirableStatuses.Contains(s.Status), SubscriptionStatus.Expired);

    public Task<bool> TryChangePlanAsync(Guid subscriptionId, Guid newPlanId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_subscriptionsById.TryGetValue(subscriptionId, out var subscription) || subscription.Status != SubscriptionStatus.Active)
        {
            return Task.FromResult(false);
        }

        SetProperty(subscription, nameof(Subscription.PlanId), newPlanId);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyCollection<Subscription>> GetActiveExpiredAsOfAsync(DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Subscription>>(
            _subscriptionsById.Values.Where(s => s.Status == SubscriptionStatus.Active && s.EndDate <= utcNow).ToList());

    private Task<bool> TryTransition(Guid subscriptionId, Func<Subscription, bool> guard, SubscriptionStatus to)
    {
        if (!_subscriptionsById.TryGetValue(subscriptionId, out var subscription) || !guard(subscription))
        {
            return Task.FromResult(false);
        }

        SetProperty(subscription, nameof(Subscription.Status), to);
        return Task.FromResult(true);
    }

    private static void SetProperty(Subscription subscription, string propertyName, object? value) =>
        typeof(Subscription).GetProperty(propertyName)!.SetValue(subscription, value);
}
