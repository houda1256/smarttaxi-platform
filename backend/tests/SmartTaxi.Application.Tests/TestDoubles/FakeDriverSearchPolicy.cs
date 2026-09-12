using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDriverSearchPolicy : IDriverSearchPolicy
{
    public IReadOnlyList<int> ProgressiveSearchRadiusKm { get; init; } = [3, 5, 10, 15];

    public int DriverResponseTimeoutSeconds { get; init; } = 20;
}
