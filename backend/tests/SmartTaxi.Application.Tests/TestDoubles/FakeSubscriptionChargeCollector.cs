using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.SubscriptionCharges.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSubscriptionChargeCollector : ISubscriptionChargeCollector
{
    public bool ShouldFail { get; set; }

    public List<(Guid SubscriberId, Guid SubscriptionId, decimal Amount, string Currency)> Charges { get; } = [];

    public Task<Result<Guid>> ChargeAsync(
        Guid subscriberId, Guid subscriptionId, decimal amount, string currency, DateTime utcNow, CancellationToken cancellationToken)
    {
        Charges.Add((subscriberId, subscriptionId, amount, currency));

        return Task.FromResult(ShouldFail
            ? Result<Guid>.Failure("Paiement refusé.", ErrorType.Validation)
            : Result<Guid>.Success(Guid.NewGuid()));
    }
}
