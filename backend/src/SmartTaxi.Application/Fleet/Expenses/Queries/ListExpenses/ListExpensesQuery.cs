using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.Application.Fleet.Expenses.Queries.ListExpenses;

public sealed record ListExpensesQuery(
    Guid OwnerId,
    Guid? FleetId,
    Guid? VehicleId,
    Guid? DriverId,
    ExpenseCategory? Category,
    ExpenseStatus? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int PageNumber,
    int PageSize) : IQuery<PagedResult<FleetExpenseSummary>>;
