using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSubscriptionEntitlementService : ISubscriptionEntitlementService
{
    public SubscriptionEntitlement? Entitlement { get; set; }

    public Task<bool> HasActiveSubscriptionAsync(Guid subscriberId, UserRole targetRole, CancellationToken cancellationToken) =>
        Task.FromResult(Entitlement is not null);

    public Task<SubscriptionEntitlement?> GetEntitlementAsync(Guid subscriberId, UserRole targetRole, CancellationToken cancellationToken) =>
        Task.FromResult(Entitlement);
}
