using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Contracts.Events;

public sealed record OwnerDriverContractActivated(Guid ContractId, DateTime OccurredAtUtc) : IDomainEvent;
