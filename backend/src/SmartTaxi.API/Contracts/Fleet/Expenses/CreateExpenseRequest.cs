namespace SmartTaxi.API.Contracts.Fleet.Expenses;

public sealed record CreateExpenseRequest(
    Guid? FleetId,
    Guid? VehicleId,
    Guid? DriverId,
    string Category,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string? Description,
    string? ReceiptReference);
