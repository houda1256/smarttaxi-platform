using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.SubmitContract;

public sealed class SubmitContractCommandHandler : ICommandHandler<SubmitContractCommand, Result>
{
    private const string NotFoundError = "Contrat introuvable.";
    private const string NotDraftError = "Ce contrat n'est pas à l'état de brouillon.";

    private readonly IDriverOwnerContractRepository _repository;

    public SubmitContractCommandHandler(IDriverOwnerContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SubmitContractCommand command, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(command.ContractId, cancellationToken);

        if (contract is null || contract.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (contract.Status != ContractStatus.Draft)
        {
            return Result.Failure(NotDraftError, ErrorType.Conflict);
        }

        var submitted = await _repository.TrySubmitAsync(contract.Id, DateTime.UtcNow, cancellationToken);

        if (!submitted)
        {
            return Result.Failure(NotDraftError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
