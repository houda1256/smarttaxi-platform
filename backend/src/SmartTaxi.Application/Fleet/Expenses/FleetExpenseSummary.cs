using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.Application.Fleet.Expenses;

public sealed record FleetExpenseSummary(
    Guid Id, Guid OwnerId, Guid? FleetId, Guid? VehicleId, Guid? DriverId, ExpenseCategory Category,
    decimal Amount, string Currency, DateOnly ExpenseDate, string? Description, string? ReceiptReference,
    ExpenseStatus Status, Guid CreatedBy, DateTime CreatedAt)
{
    public static FleetExpenseSummary FromEntity(FleetExpense expense) => new(
        expense.Id, expense.OwnerId, expense.FleetId, expense.VehicleId, expense.DriverId, expense.Category,
        expense.Money.Amount, expense.Money.Currency, expense.ExpenseDate, expense.Description,
        expense.ReceiptReference, expense.Status, expense.CreatedBy, expense.CreatedAt);
}
