using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.Application.Fleet.Expenses.Commands.CreateExpense;

public sealed record CreateExpenseCommand(
    Guid OwnerId,
    Guid? FleetId,
    Guid? VehicleId,
    Guid? DriverId,
    ExpenseCategory Category,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string? Description,
    string? ReceiptReference) : ICommand<Result<Guid>>;
