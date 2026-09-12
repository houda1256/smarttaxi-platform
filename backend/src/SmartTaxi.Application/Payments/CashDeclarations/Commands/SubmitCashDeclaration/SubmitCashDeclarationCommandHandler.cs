using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Enums;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.SubmitCashDeclaration;

/// <summary>
/// The operating model is derived from the Driver's active DriverOwnerContract
/// with this Owner, exactly like RevenueSharingCalculator derives the revenue
/// split — never chosen by the submitter, since it determines who owes whom.
/// A percentage-per-ride contract (or no contract at all, e.g. an independent
/// driver) means the Driver already keeps the cash and owes their share
/// (Model A); any period-based contract (salary/rental) means the per-ride
/// cash isn't the Driver's to keep and must be fully remitted (Model B).
/// </summary>
public sealed class SubmitCashDeclarationCommandHandler : ICommandHandler<SubmitCashDeclarationCommand, Result<Guid>>
{
    private readonly ICashDeclarationRepository _declarationRepository;
    private readonly IDriverOwnerContractRepository _contractRepository;

    public SubmitCashDeclarationCommandHandler(ICashDeclarationRepository declarationRepository, IDriverOwnerContractRepository contractRepository)
    {
        _declarationRepository = declarationRepository;
        _contractRepository = contractRepository;
    }

    public async Task<Result<Guid>> Handle(SubmitCashDeclarationCommand command, CancellationToken cancellationToken)
    {
        var contract = await _contractRepository.GetActiveForDriverAndOwnerAsync(command.DriverId, command.OwnerId, cancellationToken);

        var operatingModel = contract is not null && contract.ContractType != ContractType.PercentagePerRide
            ? CashDeclarationOperatingModel.DriverRemitsCashToOwner
            : CashDeclarationOperatingModel.DriverKeepsCashOwesShare;

        CashDeclaration declaration;

        try
        {
            declaration = CashDeclaration.Submit(
                command.DriverId, command.AssignmentId, command.PeriodStart, command.PeriodEnd,
                command.ExpectedCash, command.DeclaredCash, operatingModel, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _declarationRepository.AddAsync(declaration, cancellationToken);

        return Result<Guid>.Success(declaration.Id);
    }
}
