namespace SmartTaxi.Infrastructure.Rides.Options;

public sealed class SharedRideMatchingOptions
{
    public const string SectionName = "SharedRideMatching";

    public double MaxPickupDistanceKm { get; init; } = 1;
    public int MaxRequestWindowMinutes { get; init; } = 5;
    public double MaxDetourPercentage { get; init; } = 25;
    public int MatchExpiryMinutes { get; init; } = 5;
}
