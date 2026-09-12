using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Contracts.Events;

public sealed record OwnerDriverContractCreated(Guid ContractId, Guid OwnerId, Guid DriverId, DateTime OccurredAtUtc) : IDomainEvent;
