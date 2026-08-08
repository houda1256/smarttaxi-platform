using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Vehicles.Events;

public sealed record VehicleRejected(Guid VehicleId, Guid RejectedBy, string Reason, DateTime OccurredAtUtc) : IDomainEvent;
