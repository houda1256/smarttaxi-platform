using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.ReconcileCashRegisterSession;

public sealed record ReconcileCashRegisterSessionCommand(Guid CashRegisterSessionId, Guid ReconciledBy) : ICommand<Result>;
