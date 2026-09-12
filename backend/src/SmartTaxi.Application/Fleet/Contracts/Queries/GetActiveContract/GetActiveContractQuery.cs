using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts;

namespace SmartTaxi.Application.Fleet.Contracts.Queries.GetActiveContract;

public sealed record GetActiveContractQuery(Guid RequestingUserId, Guid DriverId, Guid OwnerId) : IQuery<Result<ContractSummary>>;
