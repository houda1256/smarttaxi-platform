using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFareCalculator : IFareCalculator
{
    public FareBreakdown Calculate(FareCalculationInput input)
    {
        var distanceFare = input.DistanceKm * 0.8m;
        var durationFare = input.DurationMinutes * 0.1m;
        var total = (2m + distanceFare + durationFare + 1m) * input.DynamicMultiplier;

        return new FareBreakdown(2m, distanceFare, durationFare, 1m, 0m, total - (2m + distanceFare + durationFare + 1m), 0m, 0m, 0m, total);
    }
}
