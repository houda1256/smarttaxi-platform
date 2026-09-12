using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IRideRepository
{
    Task AddAsync(Ride ride, CancellationToken cancellationToken);

    Task<Ride?> GetByIdAsync(Guid rideId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Ride>> GetForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Ride>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken);

    /// <summary>Non-terminal Rides for a Driver — used both for "no conflicting active Ride" eligibility and driver-lifecycle ownership checks.</summary>
    Task<Ride?> GetActiveForDriverAsync(Guid driverId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Ride>> GetAllAsync(RideStatus? status, CancellationToken cancellationToken);

    /// <summary>All Rides currently in a non-terminal status — backs the admin "active rides" dashboard.</summary>
    Task<IReadOnlyCollection<Ride>> GetActiveAsync(CancellationToken cancellationToken);

    /// <summary>RideType.Shared Rides still open for pairing (Searching or DriversAvailable) — candidate pool for shared-ride matching.</summary>
    Task<IReadOnlyCollection<Ride>> GetPendingSharedRidesAsync(CancellationToken cancellationToken);

    Task UpdateAsync(Ride ride, CancellationToken cancellationToken);

    /// <summary>Generic atomic transition: applies the Status change (if the current Status equals `from`) and writes the matching RideStatusHistory row in the same call.</summary>
    Task<bool> TryTransitionAsync(
        Guid rideId, RideStatus from, RideStatus to, Guid? changedBy, string? reason, DateTime utcNow,
        CancellationToken cancellationToken);

    /// <summary>Guarded by Status == DriversAvailable; sets SelectedDriverId/VehicleId and moves Status to PendingDriverResponse (writing history rows for both the DriverSelected and PendingDriverResponse transitions).</summary>
    Task<bool> TrySelectDriverAsync(Guid rideId, Guid driverId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Sets the immutable negotiated price once a fare proposal is accepted — simple field write, no status guard.</summary>
    Task<bool> TryApplyNegotiatedFareAsync(Guid rideId, decimal amount, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Shared-ride path: the Driver has already explicitly accepted at the
    /// SharedRideMatch level (for both paired Rides at once), so this jumps
    /// straight to DriverAccepted — guarded by Status IN (Searching,
    /// DriversAvailable) so a Ride that already went through its own
    /// individual selection/hold flow can never be silently overridden.
    /// </summary>
    Task<bool> TryConfirmSharedRideDriverAsync(
        Guid rideId, Guid driverId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryUpdateLastKnownLocationAsync(
        Guid rideId, double latitude, double longitude, DateTime recordedAt, CancellationToken cancellationToken);

    Task<bool> TryMarkDriverArrivedAsync(Guid rideId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryCompleteAsync(
        Guid rideId, decimal actualDistanceKm, int actualDurationMinutes, decimal finalFare, DateTime utcNow,
        CancellationToken cancellationToken);
}
