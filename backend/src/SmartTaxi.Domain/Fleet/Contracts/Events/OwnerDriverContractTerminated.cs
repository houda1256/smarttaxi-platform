using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Contracts.Events;

public sealed record OwnerDriverContractTerminated(Guid ContractId, DateTime OccurredAtUtc) : IDomainEvent;
