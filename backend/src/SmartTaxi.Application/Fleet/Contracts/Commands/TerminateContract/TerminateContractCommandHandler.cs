using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.TerminateContract;

public sealed class TerminateContractCommandHandler : ICommandHandler<TerminateContractCommand, Result>
{
    private const string NotFoundError = "Contrat introuvable.";
    private const string NotTerminableError = "Seul un contrat actif ou suspendu peut être résilié.";

    private readonly IDriverOwnerContractRepository _repository;

    public TerminateContractCommandHandler(IDriverOwnerContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(TerminateContractCommand command, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(command.ContractId, cancellationToken);

        if (contract is null || contract.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (contract.Status is not (ContractStatus.Active or ContractStatus.Suspended))
        {
            return Result.Failure(NotTerminableError, ErrorType.Conflict);
        }

        var terminated = await _repository.TryTerminateAsync(contract.Id, DateTime.UtcNow, cancellationToken);

        if (!terminated)
        {
            return Result.Failure(NotTerminableError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
