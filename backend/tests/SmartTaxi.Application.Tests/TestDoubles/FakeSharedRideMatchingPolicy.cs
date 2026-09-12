using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSharedRideMatchingPolicy : ISharedRideMatchingPolicy
{
    public double MaxPickupDistanceKm { get; init; } = 1;
    public int MaxRequestWindowMinutes { get; init; } = 5;
    public double MaxDetourPercentage { get; init; } = 25;
    public TimeSpan MatchExpiry { get; init; } = TimeSpan.FromMinutes(5);
}
