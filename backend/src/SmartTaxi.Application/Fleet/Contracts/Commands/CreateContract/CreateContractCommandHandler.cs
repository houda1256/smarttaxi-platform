using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Entities;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.CreateContract;

public sealed class CreateContractCommandHandler : ICommandHandler<CreateContractCommand, Result<Guid>>
{
    private const string DuplicateActiveContractError =
        "Un contrat actif ou en attente de signature existe déjà pour ce chauffeur et ce propriétaire.";

    private readonly IDriverOwnerContractRepository _repository;

    public CreateContractCommandHandler(IDriverOwnerContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateContractCommand command, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetActiveOrPendingForDriverOwnerAsync(command.DriverId, command.OwnerId, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure(DuplicateActiveContractError, ErrorType.Conflict);
        }

        DriverOwnerContract contract;

        try
        {
            contract = DriverOwnerContract.CreateDraft(
                command.OwnerId, command.DriverId, command.VehicleId, command.ContractType, command.StartDate,
                command.EndDate, command.FixedAmount, command.DriverPercentage, command.OwnerPercentage,
                command.PaymentFrequency, command.DocumentReference, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.AddAsync(contract, cancellationToken);

        return Result<Guid>.Success(contract.Id);
    }
}
