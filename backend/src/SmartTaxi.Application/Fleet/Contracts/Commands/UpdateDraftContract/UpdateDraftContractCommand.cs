using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Fleet.Contracts.Commands.UpdateDraftContract;

public sealed record UpdateDraftContractCommand(
    Guid RequestingUserId,
    Guid ContractId,
    ContractType ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? FixedAmount,
    decimal? DriverPercentage,
    decimal? OwnerPercentage,
    PaymentFrequency PaymentFrequency,
    string? DocumentReference) : ICommand<Result>;
