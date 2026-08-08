namespace SmartTaxi.API.Contracts.Fleet.Contracts;

public sealed record CreateContractRequest(
    Guid DriverId,
    Guid? VehicleId,
    string ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? FixedAmount,
    decimal? DriverPercentage,
    decimal? OwnerPercentage,
    string PaymentFrequency,
    string? DocumentReference);
