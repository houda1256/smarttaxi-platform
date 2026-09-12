using SmartTaxi.Application.Payments.Payouts.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePayoutPolicy : IPayoutPolicy
{
    public decimal MinimumPayoutAmount { get; init; } = 20m;
}
