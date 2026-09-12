using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Fleet.Contracts.Queries.GetActiveContract;

/// <summary>Visible to the owner or to the driver themselves — never to an unrelated caller.</summary>
public sealed class GetActiveContractQueryHandler : IQueryHandler<GetActiveContractQuery, Result<ContractSummary>>
{
    private const string NotFoundError = "Contrat introuvable.";

    private readonly IDriverOwnerContractRepository _repository;
    private readonly IDriverProfileRepository _driverRepository;

    public GetActiveContractQueryHandler(IDriverOwnerContractRepository repository, IDriverProfileRepository driverRepository)
    {
        _repository = repository;
        _driverRepository = driverRepository;
    }

    public async Task<Result<ContractSummary>> Handle(GetActiveContractQuery query, CancellationToken cancellationToken)
    {
        if (query.RequestingUserId != query.OwnerId)
        {
            var driver = await _driverRepository.GetByIdAsync(query.DriverId, cancellationToken);

            if (driver is null || driver.UserId != query.RequestingUserId)
            {
                return Result<ContractSummary>.Failure(NotFoundError, ErrorType.NotFound);
            }
        }

        var contract = await _repository.GetActiveOrPendingForDriverOwnerAsync(query.DriverId, query.OwnerId, cancellationToken);

        if (contract is null || contract.Status != ContractStatus.Active)
        {
            return Result<ContractSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        return Result<ContractSummary>.Success(ContractSummary.FromEntity(contract));
    }
}
