using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Vehicles.Events;

public sealed record VehicleRetired(Guid VehicleId, DateTime OccurredAtUtc) : IDomainEvent;
