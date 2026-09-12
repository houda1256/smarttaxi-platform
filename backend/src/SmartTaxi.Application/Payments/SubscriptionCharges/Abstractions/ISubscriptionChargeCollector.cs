using SmartTaxi.Application.Common;

namespace SmartTaxi.Application.Payments.SubscriptionCharges.Abstractions;

/// <summary>
/// The single entry point Subscription Management uses to collect payment
/// for a plan charge (initial subscribe or a renewal) — the "existing
/// Payment application/service abstraction" the module spec requires
/// Subscription to go through, so it never manipulates Payment/Invoice
/// tables directly. Deterministic and testable per the project's rule
/// against silent-success stubs: it always persists a SubscriptionCharge
/// row and only fails for invalid amounts/currency (Money.Create's own
/// validation), same posture as the other Dev* simulated integrations.
/// </summary>
public interface ISubscriptionChargeCollector
{
    Task<Result<Guid>> ChargeAsync(
        Guid subscriberId, Guid subscriptionId, decimal amount, string currency, DateTime utcNow,
        CancellationToken cancellationToken);
}
