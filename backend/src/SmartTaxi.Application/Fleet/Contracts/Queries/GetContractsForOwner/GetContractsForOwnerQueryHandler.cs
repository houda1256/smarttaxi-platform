using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;

namespace SmartTaxi.Application.Fleet.Contracts.Queries.GetContractsForOwner;

public sealed class GetContractsForOwnerQueryHandler : IQueryHandler<GetContractsForOwnerQuery, IReadOnlyCollection<ContractSummary>>
{
    private readonly IDriverOwnerContractRepository _repository;

    public GetContractsForOwnerQueryHandler(IDriverOwnerContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<ContractSummary>> Handle(GetContractsForOwnerQuery query, CancellationToken cancellationToken)
    {
        var contracts = await _repository.GetForOwnerAsync(query.OwnerId, cancellationToken);

        return contracts.Select(ContractSummary.FromEntity).ToList();
    }
}
