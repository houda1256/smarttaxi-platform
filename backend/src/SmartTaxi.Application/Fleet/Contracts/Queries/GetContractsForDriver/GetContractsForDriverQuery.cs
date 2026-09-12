using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts;

namespace SmartTaxi.Application.Fleet.Contracts.Queries.GetContractsForDriver;

public sealed record GetContractsForDriverQuery(Guid DriverId) : IQuery<IReadOnlyCollection<ContractSummary>>;
