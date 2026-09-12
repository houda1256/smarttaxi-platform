using SmartTaxi.Application.Fleet.Expenses;

namespace SmartTaxi.API.Contracts.Fleet.Expenses;

public sealed record ExpenseResponse(
    Guid Id, Guid OwnerId, Guid? FleetId, Guid? VehicleId, Guid? DriverId, string Category, decimal Amount,
    string Currency, DateOnly ExpenseDate, string? Description, string? ReceiptReference, string Status,
    Guid CreatedBy, DateTime CreatedAt)
{
    public static ExpenseResponse FromSummary(FleetExpenseSummary summary) => new(
        summary.Id, summary.OwnerId, summary.FleetId, summary.VehicleId, summary.DriverId, summary.Category.ToString(),
        summary.Amount, summary.Currency, summary.ExpenseDate, summary.Description, summary.ReceiptReference,
        summary.Status.ToString(), summary.CreatedBy, summary.CreatedAt);
}
