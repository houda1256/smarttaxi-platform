using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Rides;

/// <summary>
/// The single source of truth for which Ride status transitions are legal.
/// Every atomic repository-level transition (Application/Infrastructure)
/// encodes its own specific "from" status as a WHERE guard already, so this
/// map is not consulted at runtime by those — it exists so the full graph is
/// documented in one place, is unit-testable, and is available to the one
/// place that genuinely needs an arbitrary-from check: an admin override.
/// </summary>
public static class RideStatusTransitions
{
    // DriverNoShow, like DriverRejected, is transient — it always cascades straight on to
    // DriversAvailable in the same call, so it is deliberately not "terminal": the Ride keeps
    // its active slot while a new Driver is found. CustomerNoShow has no such cascade — nobody
    // is left to pick up, so the Ride genuinely ends there.
    private static readonly RideStatus[] TerminalStatuses =
    [
        RideStatus.Completed, RideStatus.CancelledByCustomer, RideStatus.CancelledByDriver,
        RideStatus.CancelledByAdmin, RideStatus.Expired, RideStatus.CustomerNoShow
    ];

    private static readonly Dictionary<RideStatus, RideStatus[]> Allowed = new()
    {
        [RideStatus.Draft] = [RideStatus.Searching, RideStatus.CancelledByCustomer, RideStatus.Expired],
        [RideStatus.Searching] =
            [RideStatus.DriversAvailable, RideStatus.NoDriverAvailable, RideStatus.CancelledByCustomer, RideStatus.Expired],
        [RideStatus.DriversAvailable] =
            [RideStatus.DriverSelected, RideStatus.NoDriverAvailable, RideStatus.CancelledByCustomer, RideStatus.Expired],
        [RideStatus.DriverSelected] = [RideStatus.PendingDriverResponse, RideStatus.CancelledByCustomer],
        [RideStatus.PendingDriverResponse] =
            [RideStatus.DriverAccepted, RideStatus.DriverRejected, RideStatus.CancelledByCustomer, RideStatus.Expired],
        [RideStatus.DriverAccepted] =
            [RideStatus.DriverEnRoute, RideStatus.CancelledByCustomer, RideStatus.CancelledByDriver, RideStatus.DriverNoShow],
        [RideStatus.DriverRejected] =
            [RideStatus.DriversAvailable, RideStatus.NoDriverAvailable, RideStatus.CancelledByCustomer],
        [RideStatus.DriverEnRoute] =
            [RideStatus.DriverArrived, RideStatus.CancelledByCustomer, RideStatus.CancelledByDriver, RideStatus.DriverNoShow],
        [RideStatus.DriverArrived] =
            [RideStatus.PassengerOnBoard, RideStatus.CustomerNoShow, RideStatus.CancelledByCustomer, RideStatus.CancelledByDriver],
        [RideStatus.PassengerOnBoard] = [RideStatus.InProgress, RideStatus.CancelledByDriver],
        [RideStatus.InProgress] = [RideStatus.AwaitingPayment, RideStatus.Disputed],
        [RideStatus.AwaitingPayment] = [RideStatus.Completed, RideStatus.Disputed],
        [RideStatus.Completed] = [RideStatus.Disputed],
        [RideStatus.CancelledByCustomer] = [],
        [RideStatus.CancelledByDriver] = [],
        [RideStatus.CancelledByAdmin] = [],
        [RideStatus.Expired] = [],
        // A failed search can be retried without creating a new Ride.
        [RideStatus.NoDriverAvailable] = [RideStatus.Searching],
        [RideStatus.CustomerNoShow] = [],
        // Reported driver no-show returns the Ride to the recommendation pool.
        [RideStatus.DriverNoShow] = [RideStatus.DriversAvailable],
        [RideStatus.Disputed] = [RideStatus.Completed, RideStatus.CancelledByAdmin]
    };

    public static bool CanTransition(RideStatus from, RideStatus to)
    {
        if (to == RideStatus.CancelledByAdmin && !TerminalStatuses.Contains(from))
        {
            return true;
        }

        return Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    public static IReadOnlyCollection<RideStatus> GetAllowedNext(RideStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : [];

    public static bool IsTerminal(RideStatus status) => TerminalStatuses.Contains(status);
}
