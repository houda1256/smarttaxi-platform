using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePlatformCommissionPolicy : IPlatformCommissionPolicy
{
    public decimal CommissionPercentage { get; init; } = 10m;
}
