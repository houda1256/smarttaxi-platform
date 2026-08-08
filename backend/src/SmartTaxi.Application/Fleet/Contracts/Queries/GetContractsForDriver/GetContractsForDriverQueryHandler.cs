using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;

namespace SmartTaxi.Application.Fleet.Contracts.Queries.GetContractsForDriver;

public sealed class GetContractsForDriverQueryHandler : IQueryHandler<GetContractsForDriverQuery, IReadOnlyCollection<ContractSummary>>
{
    private readonly IDriverOwnerContractRepository _repository;

    public GetContractsForDriverQueryHandler(IDriverOwnerContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<ContractSummary>> Handle(GetContractsForDriverQuery query, CancellationToken cancellationToken)
    {
        var contracts = await _repository.GetForDriverAsync(query.DriverId, cancellationToken);

        return contracts.Select(ContractSummary.FromEntity).ToList();
    }
}
