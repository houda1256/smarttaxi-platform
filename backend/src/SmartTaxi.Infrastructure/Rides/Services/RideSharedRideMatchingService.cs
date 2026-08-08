using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Infrastructure.Rides.Services;

/// <summary>
/// Deterministic, rule-based compatibility check — never ML. Directional
/// compatibility and detour tolerance are combined into a single check: the
/// distance between the two destinations, relative to the longer of the two
/// individual trips, must stay within the configured detour percentage.
/// </summary>
internal sealed class RideSharedRideMatchingService : ISharedRideMatchingService
{
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly ISharedRideMatchingPolicy _policy;

    public RideSharedRideMatchingService(IDistanceCalculator distanceCalculator, ISharedRideMatchingPolicy policy)
    {
        _distanceCalculator = distanceCalculator;
        _policy = policy;
    }

    public SharedRideCompatibilityReport CheckCompatibility(SharedRideCompatibilityInput input)
    {
        var reasons = new List<string>();

        var pickupDistanceKm = (double)_distanceCalculator.CalculateKilometers(input.PickupA, input.PickupB);

        if (pickupDistanceKm > _policy.MaxPickupDistanceKm)
        {
            reasons.Add($"Distance entre points de prise en charge trop grande ({pickupDistanceKm:F2} km).");
        }

        var requestWindowMinutes = Math.Abs((input.RequestedAtA - input.RequestedAtB).TotalMinutes);

        if (requestWindowMinutes > _policy.MaxRequestWindowMinutes)
        {
            reasons.Add($"Demandes trop éloignées dans le temps ({requestWindowMinutes:F0} min).");
        }

        var tripDistanceA = (double)_distanceCalculator.CalculateKilometers(input.PickupA, input.DestinationA);
        var tripDistanceB = (double)_distanceCalculator.CalculateKilometers(input.PickupB, input.DestinationB);
        var destinationDistanceKm = (double)_distanceCalculator.CalculateKilometers(input.DestinationA, input.DestinationB);
        var longestTripKm = Math.Max(tripDistanceA, tripDistanceB);
        var detourPercentage = longestTripKm > 0 ? destinationDistanceKm / longestTripKm * 100 : 0;

        if (detourPercentage > _policy.MaxDetourPercentage)
        {
            reasons.Add($"Détour estimé trop important ({detourPercentage:F0}%).");
        }

        if (input.PassengerCountA + input.PassengerCountB > input.VehicleSeatCount)
        {
            reasons.Add("Capacité de sièges insuffisante pour les deux passagers.");
        }

        return new SharedRideCompatibilityReport(reasons.Count == 0, reasons);
    }
}
