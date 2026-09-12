using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.UpdateDraftContract;

public sealed class UpdateDraftContractCommandHandler : ICommandHandler<UpdateDraftContractCommand, Result>
{
    private const string NotFoundError = "Contrat introuvable.";

    private readonly IDriverOwnerContractRepository _repository;

    public UpdateDraftContractCommandHandler(IDriverOwnerContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateDraftContractCommand command, CancellationToken cancellationToken)
    {
        var contract = await _repository.GetByIdAsync(command.ContractId, cancellationToken);

        if (contract is null || contract.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            contract.UpdateDraftTerms(
                command.ContractType, command.StartDate, command.EndDate, command.FixedAmount,
                command.DriverPercentage, command.OwnerPercentage, command.PaymentFrequency,
                command.DocumentReference, DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.UpdateAsync(contract, cancellationToken);

        return Result.Success();
    }
}
