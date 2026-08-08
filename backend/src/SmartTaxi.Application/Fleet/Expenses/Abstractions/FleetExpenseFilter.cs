using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.Application.Fleet.Expenses.Abstractions;

public sealed record FleetExpenseFilter(
    Guid OwnerId,
    Guid? FleetId = null,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    ExpenseCategory? Category = null,
    ExpenseStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);
