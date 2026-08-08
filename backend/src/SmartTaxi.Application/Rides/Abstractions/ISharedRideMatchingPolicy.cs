namespace SmartTaxi.Application.Rides.Abstractions;

public interface ISharedRideMatchingPolicy
{
    double MaxPickupDistanceKm { get; }
    int MaxRequestWindowMinutes { get; }
    double MaxDetourPercentage { get; }
    TimeSpan MatchExpiry { get; }
}
