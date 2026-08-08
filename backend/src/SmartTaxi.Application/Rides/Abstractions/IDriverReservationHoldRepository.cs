using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Abstractions;

public interface IDriverReservationHoldRepository
{
    /// <summary>May throw a unique-violation-mapped Conflict if another Active hold already exists for this Driver — see the DB partial unique index on DriverId WHERE Status = 'Active'.</summary>
    Task<bool> TryAddAsync(DriverReservationHold hold, CancellationToken cancellationToken);

    Task<DriverReservationHold?> GetByIdAsync(Guid holdId, CancellationToken cancellationToken);

    Task<DriverReservationHold?> GetActiveForRideAsync(Guid rideId, CancellationToken cancellationToken);

    Task<DriverReservationHold?> GetActiveForDriverAsync(Guid driverId, CancellationToken cancellationToken);

    Task<bool> TryAcceptAsync(Guid holdId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryReleaseAsync(Guid holdId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryExpireAsync(Guid holdId, DateTime utcNow, CancellationToken cancellationToken);
}
