using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.ActivateContract;

/// <summary>
/// No real digital signature infrastructure — activation is a manual
/// confirmation step, gated on a signed-document reference already having
/// been attached (via UpdateDraftContract/DocumentReference) before submission.
/// </summary>
public sealed class ActivateContractCommandHandler : ICommandHandler<ActivateContractCommand, Result>
{
    private const string NotFoundError = "Contrat introuvable.";
    private const string NotPendingSignatureError = "Ce contrat n'est pas en attente de signature.";
    private const string MissingDocumentError = "Une référence au document signé est requise avant activation.";
    private const string DuplicateActiveContractError =
        "Un autre contrat actif existe déjà pour ce chauffeur et ce propriétaire.";

    private readonly IDriverOwnerContractRepository _repository;

    public ActivateContractCommandHandler(IDriverOwnerContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ActivateContractCommand command, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(command.ContractId, cancellationToken);

        if (contract is null || contract.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (contract.Status != ContractStatus.PendingSignature)
        {
            return Result.Failure(NotPendingSignatureError, ErrorType.Conflict);
        }

        if (string.IsNullOrWhiteSpace(contract.DocumentReference))
        {
            return Result.Failure(MissingDocumentError, ErrorType.Validation);
        }

        var existingActive = await _repository.GetActiveOrPendingForDriverOwnerAsync(
            contract.DriverId, contract.OwnerId, cancellationToken);

        if (existingActive is not null && existingActive.Id != contract.Id && existingActive.Status == ContractStatus.Active)
        {
            return Result.Failure(DuplicateActiveContractError, ErrorType.Conflict);
        }

        var activated = await _repository.TryActivateAsync(contract.Id, DateTime.UtcNow, cancellationToken);

        if (!activated)
        {
            return Result.Failure(NotPendingSignatureError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
