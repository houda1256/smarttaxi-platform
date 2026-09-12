using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Owners.Events;

public sealed record TaxiOwnerProfileCreated(Guid OwnerId, DateTime OccurredAtUtc) : IDomainEvent;
