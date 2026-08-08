namespace SmartTaxi.Infrastructure.Rides.Options;

public sealed class RideDriverSearchOptions
{
    public const string SectionName = "RideDriverSearch";

    public int[] ProgressiveSearchRadiusKm { get; init; } = [3, 5, 10, 15];

    public int DriverResponseTimeoutSeconds { get; init; } = 20;
}
