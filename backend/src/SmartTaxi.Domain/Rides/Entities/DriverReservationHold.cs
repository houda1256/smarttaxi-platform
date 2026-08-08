using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.Events;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>
/// The DB-level defense against two Customers reserving the same Driver
/// concurrently is a partial unique index on DriverId WHERE Status = 'Active'
/// (Infrastructure) — this entity itself only enforces the shape of a single
/// hold. Accept/Release/Expire are atomic repository-level guards, not domain
/// methods, same pattern as every Fleet status transition.
/// </summary>
public sealed class DriverReservationHold : AggregateRoot
{
    public Guid RideId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid VehicleId { get; private set; }
    public DateTime HeldAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DriverReservationHoldStatus Status { get; private set; }

    private DriverReservationHold()
    {
    }

    private DriverReservationHold(Guid rideId, Guid driverId, Guid vehicleId, DateTime heldAt, TimeSpan holdDuration)
        : base(Guid.NewGuid())
    {
        RideId = rideId;
        DriverId = driverId;
        VehicleId = vehicleId;
        HeldAt = heldAt;
        ExpiresAt = heldAt.Add(holdDuration);
        Status = DriverReservationHoldStatus.Active;

        RaiseDomainEvent(new DriverReservationHeld(Id, rideId, driverId, heldAt));
    }

    public static DriverReservationHold CreateActive(Guid rideId, Guid driverId, Guid vehicleId, DateTime utcNow, TimeSpan holdDuration)
    {
        if (holdDuration <= TimeSpan.Zero)
        {
            throw new ArgumentException("La durée de réservation doit être positive.");
        }

        return new DriverReservationHold(rideId, driverId, vehicleId, utcNow, holdDuration);
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
}
