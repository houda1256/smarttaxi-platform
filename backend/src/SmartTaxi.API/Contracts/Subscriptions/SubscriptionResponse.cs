using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.API.Contracts.Subscriptions;

public sealed record SubscriptionResponse(
    Guid Id,
    Guid SubscriberId,
    Guid PlanId,
    UserRole TargetRole,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    DateTime? TrialEndsAt,
    bool AutoRenew,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static SubscriptionResponse FromEntity(Subscription subscription) => new(
        subscription.Id, subscription.SubscriberId, subscription.PlanId, subscription.TargetRole, subscription.Status,
        subscription.StartDate, subscription.EndDate, subscription.TrialEndsAt, subscription.AutoRenew,
        subscription.CreatedAt, subscription.UpdatedAt);
}
