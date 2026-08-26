using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Subscriptions.Events;

/// <summary>Genuinely raised on creation (unit-testable). Every other event below is documentation-only, never raised — status transitions bypass entity mutation, same convention as Payment/Ride.</summary>
public sealed record SubscriptionCreated(Guid SubscriptionId, Guid SubscriberId, Guid PlanId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SubscriptionActivated(Guid SubscriptionId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SubscriptionRenewed(Guid SubscriptionId, DateTime NewEndDate, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SubscriptionSuspended(Guid SubscriptionId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SubscriptionCancelled(Guid SubscriptionId, string? Reason, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SubscriptionExpired(Guid SubscriptionId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SubscriptionUpgraded(Guid SubscriptionId, Guid FromPlanId, Guid ToPlanId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SubscriptionDowngraded(Guid SubscriptionId, Guid FromPlanId, Guid ToPlanId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SubscriptionPaymentFailed(Guid SubscriptionId, Guid ChargeId, DateTime OccurredAtUtc) : IDomainEvent;
