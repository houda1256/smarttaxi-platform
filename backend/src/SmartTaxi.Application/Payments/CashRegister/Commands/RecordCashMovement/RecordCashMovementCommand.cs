using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.RecordCashMovement;

public sealed record RecordCashMovementCommand(
    Guid CashRegisterSessionId, CashMovementType MovementType, decimal Amount, string? Description, Guid RecordedBy) : ICommand<Result<Guid>>;
