using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Vehicles.Events;

public sealed record VehicleRegistered(Guid VehicleId, Guid OwnerId, DateTime OccurredAtUtc) : IDomainEvent;
