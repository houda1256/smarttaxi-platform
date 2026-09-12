using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDynamicPricingProvider : IDynamicPricingProvider
{
    public decimal Multiplier { get; init; } = 1.0m;

    public DynamicPricingResult GetMultiplier(DateTime utcNow, Guid? zoneId) => new(Multiplier, "Tarif standard");
}
