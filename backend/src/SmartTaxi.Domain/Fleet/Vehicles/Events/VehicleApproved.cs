using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Vehicles.Events;

public sealed record VehicleApproved(Guid VehicleId, Guid ApprovedBy, DateTime OccurredAtUtc) : IDomainEvent;
