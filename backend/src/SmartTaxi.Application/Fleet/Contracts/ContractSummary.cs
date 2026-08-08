using SmartTaxi.Domain.Fleet.Contracts.Entities;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Fleet.Contracts;

public sealed record ContractSummary(
    Guid Id, Guid OwnerId, Guid DriverId, Guid? VehicleId, ContractType ContractType, DateOnly StartDate,
    DateOnly? EndDate, decimal? FixedAmount, decimal? DriverPercentage, decimal? OwnerPercentage,
    PaymentFrequency PaymentFrequency, string? DocumentReference, ContractStatus Status, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static ContractSummary FromEntity(DriverOwnerContract contract) => new(
        contract.Id, contract.OwnerId, contract.DriverId, contract.VehicleId, contract.ContractType,
        contract.StartDate, contract.EndDate, contract.FixedAmount, contract.DriverPercentage, contract.OwnerPercentage,
        contract.PaymentFrequency, contract.DocumentReference, contract.Status, contract.CreatedAt, contract.UpdatedAt);
}
