using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Abstractions;

public interface ISubscriptionRepository
{
    /// <summary>May return false if a Pending/Active subscription already exists for this subscriber+role — see the DB partial unique index on (SubscriberId, TargetRole).</summary>
    Task<bool> TryAddAsync(Subscription subscription, CancellationToken cancellationToken);

    Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken);

    Task<Subscription?> GetActiveOrPendingForSubscriberAsync(
        Guid subscriberId, UserRole targetRole, CancellationToken cancellationToken);

    /// <summary>Full history for a subscriber, newest first — historical rows are never deleted.</summary>
    Task<IReadOnlyCollection<Subscription>> GetHistoryForSubscriberAsync(Guid subscriberId, CancellationToken cancellationToken);

    Task<bool> TryActivateAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically extends EndDate, guarded on both Status == Active and EndDate == expectedCurrentEndDate —
    /// the compare-and-swap a caller uses to reserve a renewal before charging, so a lost race (another
    /// renewal, cancellation, or expiration touching this subscription first) is rejected here rather
    /// than after money has already moved. The same method reverts a reservation (pass the just-set
    /// EndDate as expectedCurrentEndDate and the original EndDate as newEndDate) if a charge subsequently fails.
    /// </summary>
    Task<bool> TryRenewAsync(
        Guid subscriptionId, DateTime expectedCurrentEndDate, DateTime newEndDate, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TrySuspendAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryCancelAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryExpireAsync(Guid subscriptionId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryChangePlanAsync(Guid subscriptionId, Guid newPlanId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Active subscriptions whose EndDate has already passed — the read side an expiration sweep (manual or future scheduler) uses to find work.</summary>
    Task<IReadOnlyCollection<Subscription>> GetActiveExpiredAsOfAsync(DateTime utcNow, CancellationToken cancellationToken);
}
