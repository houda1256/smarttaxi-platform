using SmartTaxi.Application.Fleet.Contracts;

namespace SmartTaxi.API.Contracts.Fleet.Contracts;

public sealed record ContractResponse(
    Guid Id, Guid OwnerId, Guid DriverId, Guid? VehicleId, string ContractType, DateOnly StartDate,
    DateOnly? EndDate, decimal? FixedAmount, decimal? DriverPercentage, decimal? OwnerPercentage,
    string PaymentFrequency, string? DocumentReference, string Status, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static ContractResponse FromSummary(ContractSummary summary) => new(
        summary.Id, summary.OwnerId, summary.DriverId, summary.VehicleId, summary.ContractType.ToString(),
        summary.StartDate, summary.EndDate, summary.FixedAmount, summary.DriverPercentage, summary.OwnerPercentage,
        summary.PaymentFrequency.ToString(), summary.DocumentReference, summary.Status.ToString(), summary.CreatedAt,
        summary.UpdatedAt);
}
