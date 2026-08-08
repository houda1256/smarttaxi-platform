using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.CreateContract;

public sealed record CreateContractCommand(
    Guid OwnerId,
    Guid DriverId,
    Guid? VehicleId,
    ContractType ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? FixedAmount,
    decimal? DriverPercentage,
    decimal? OwnerPercentage,
    PaymentFrequency PaymentFrequency,
    string? DocumentReference) : ICommand<Result<Guid>>;
