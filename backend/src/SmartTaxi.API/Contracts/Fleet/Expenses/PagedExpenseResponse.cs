namespace SmartTaxi.API.Contracts.Fleet.Expenses;

public sealed record PagedExpenseResponse(IReadOnlyCollection<ExpenseResponse> Items, int TotalCount, int PageNumber, int PageSize);
