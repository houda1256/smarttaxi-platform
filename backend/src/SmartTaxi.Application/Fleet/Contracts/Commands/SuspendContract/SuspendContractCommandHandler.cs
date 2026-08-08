using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.SuspendContract;

public sealed class SuspendContractCommandHandler : ICommandHandler<SuspendContractCommand, Result>
{
    private const string NotFoundError = "Contrat introuvable.";
    private const string NotActiveError = "Seul un contrat actif peut être suspendu.";

    private readonly IDriverOwnerContractRepository _repository;

    public SuspendContractCommandHandler(IDriverOwnerContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SuspendContractCommand command, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(command.ContractId, cancellationToken);

        if (contract is null || contract.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (contract.Status != ContractStatus.Active)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        var suspended = await _repository.TrySuspendAsync(contract.Id, DateTime.UtcNow, cancellationToken);

        if (!suspended)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
