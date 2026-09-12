using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts;

namespace SmartTaxi.Application.Fleet.Contracts.Queries.GetContractsForOwner;

public sealed record GetContractsForOwnerQuery(Guid OwnerId) : IQuery<IReadOnlyCollection<ContractSummary>>;
