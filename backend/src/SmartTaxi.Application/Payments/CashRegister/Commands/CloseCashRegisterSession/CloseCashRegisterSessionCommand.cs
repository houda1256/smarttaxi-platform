using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.CloseCashRegisterSession;

public sealed record CloseCashRegisterSessionCommand(Guid CashRegisterSessionId, Guid ClosedBy, decimal ClosingActualBalance, string? DifferenceReason)
    : ICommand<Result<decimal>>;
