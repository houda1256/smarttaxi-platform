using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Subscriptions.Abstractions;

/// <summary>
/// The single entitlement service the module spec requires — every other
/// module (Ride, Fleet, ...) that needs to know whether a subscriber has an
/// active subscription, and what it grants, goes through this rather than
/// re-deriving the answer from Subscription/SubscriptionPlan rows itself.
/// Not wired into Ride/Fleet in this phase (out of scope — see phase report).
/// </summary>
public interface ISubscriptionEntitlementService
{
    Task<bool> HasActiveSubscriptionAsync(Guid subscriberId, UserRole targetRole, CancellationToken cancellationToken);

    Task<SubscriptionEntitlement?> GetEntitlementAsync(Guid subscriberId, UserRole targetRole, CancellationToken cancellationToken);
}

public sealed record SubscriptionEntitlement(
    Guid SubscriptionId,
    Guid PlanId,
    IReadOnlyList<string> Features,
    IReadOnlyDictionary<string, int> Limits,
    DateTime EndDate);
