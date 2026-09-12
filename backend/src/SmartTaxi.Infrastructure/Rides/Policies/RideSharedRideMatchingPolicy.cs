using Microsoft.Extensions.Options;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Infrastructure.Rides.Options;

namespace SmartTaxi.Infrastructure.Rides.Policies;

internal sealed class RideSharedRideMatchingPolicy : ISharedRideMatchingPolicy
{
    public double MaxPickupDistanceKm { get; }
    public int MaxRequestWindowMinutes { get; }
    public double MaxDetourPercentage { get; }
    public TimeSpan MatchExpiry { get; }

    public RideSharedRideMatchingPolicy(IOptions<SharedRideMatchingOptions> options)
    {
        var value = options.Value;
        MaxPickupDistanceKm = value.MaxPickupDistanceKm;
        MaxRequestWindowMinutes = value.MaxRequestWindowMinutes;
        MaxDetourPercentage = value.MaxDetourPercentage;
        MatchExpiry = TimeSpan.FromMinutes(value.MatchExpiryMinutes);
    }
}
