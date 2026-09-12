using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Domain.RoadsideAssistance.Policies;

/// <summary>
/// Centralized, single source of truth for which RoadsideServiceType values
/// immobilize the vehicle (and therefore require the Roadside-owned atomic
/// Fleet-coordination repositories) — approved plan decision. Never
/// duplicated as inline switch/if chains in handlers or repositories.
/// </summary>
public static class RoadsideServiceTypePolicy
{
    private static readonly IReadOnlyCollection<RoadsideServiceType> ImmobilizingServiceTypes =
    [
        RoadsideServiceType.Towing,
        RoadsideServiceType.MechanicalBreakdownAssistance,
        RoadsideServiceType.AccidentAssistance
    ];

    public static bool RequiresVehicleImmobilization(RoadsideServiceType serviceType) => ImmobilizingServiceTypes.Contains(serviceType);
}
