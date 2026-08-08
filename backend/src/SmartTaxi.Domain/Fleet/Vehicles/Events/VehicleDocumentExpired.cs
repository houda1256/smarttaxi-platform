using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Vehicles.Events;

public sealed record VehicleDocumentExpired(Guid VehicleId, Guid DocumentId, DateTime OccurredAtUtc) : IDomainEvent;
