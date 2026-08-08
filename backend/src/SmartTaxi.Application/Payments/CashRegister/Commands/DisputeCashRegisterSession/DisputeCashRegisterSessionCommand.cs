using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.DisputeCashRegisterSession;

public sealed record DisputeCashRegisterSessionCommand(Guid CashRegisterSessionId, Guid DisputedBy, string Reason) : ICommand<Result>;
