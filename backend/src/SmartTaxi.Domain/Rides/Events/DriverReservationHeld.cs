using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

public sealed record DriverReservationHeld(Guid HoldId, Guid RideId, Guid DriverId, DateTime OccurredAtUtc) : IDomainEvent;
