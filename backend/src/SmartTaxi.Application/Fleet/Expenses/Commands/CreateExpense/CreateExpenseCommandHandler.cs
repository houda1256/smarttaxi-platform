using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Expenses.Abstractions;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;
using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Fleet.Expenses.ValueObjects;

namespace SmartTaxi.Application.Fleet.Expenses.Commands.CreateExpense;

public sealed class CreateExpenseCommandHandler : ICommandHandler<CreateExpenseCommand, Result<Guid>>
{
    private const string FleetNotFoundError = "Flotte introuvable.";

    private readonly IFleetRepository _fleetRepository;
    private readonly IFleetExpenseRepository _repository;

    public CreateExpenseCommandHandler(IFleetRepository fleetRepository, IFleetExpenseRepository repository)
    {
        _fleetRepository = fleetRepository;
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateExpenseCommand command, CancellationToken cancellationToken)
    {
        if (command.FleetId is not null)
        {
            var fleet = await _fleetRepository.GetByIdAsync(command.FleetId.Value, cancellationToken);

            if (fleet is null || fleet.OwnerId != command.OwnerId)
            {
                return Result<Guid>.Failure(FleetNotFoundError, ErrorType.NotFound);
            }
        }

        if (!Money.TryCreate(command.Amount, command.Currency, out var money, out var moneyError))
        {
            return Result<Guid>.Failure(moneyError, ErrorType.Validation);
        }

        var expense = FleetExpense.Create(
            command.OwnerId, command.FleetId, command.VehicleId, command.DriverId, command.Category, money,
            command.ExpenseDate, command.Description, command.ReceiptReference, command.OwnerId, DateTime.UtcNow);

        await _repository.AddAsync(expense, cancellationToken);

        return Result<Guid>.Success(expense.Id);
    }
}
