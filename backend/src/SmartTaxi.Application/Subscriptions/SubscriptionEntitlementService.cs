using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Subscriptions;

public sealed class SubscriptionEntitlementService : ISubscriptionEntitlementService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;

    public SubscriptionEntitlementService(
        ISubscriptionRepository subscriptionRepository, ISubscriptionPlanRepository planRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid subscriberId, UserRole targetRole, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetActiveOrPendingForSubscriberAsync(subscriberId, targetRole, cancellationToken);
        return subscription is not null && subscription.IsEffectivelyActive(DateTime.UtcNow);
    }

    public async Task<SubscriptionEntitlement?> GetEntitlementAsync(
        Guid subscriberId, UserRole targetRole, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetActiveOrPendingForSubscriberAsync(subscriberId, targetRole, cancellationToken);

        if (subscription is null || !subscription.IsEffectivelyActive(DateTime.UtcNow))
        {
            return null;
        }

        var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        return new SubscriptionEntitlement(subscription.Id, plan.Id, plan.Features, plan.Limits, subscription.EndDate);
    }
}
