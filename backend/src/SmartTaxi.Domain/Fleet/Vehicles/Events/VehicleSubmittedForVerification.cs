using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Vehicles.Events;

public sealed record VehicleSubmittedForVerification(Guid VehicleId, DateTime OccurredAtUtc) : IDomainEvent;
