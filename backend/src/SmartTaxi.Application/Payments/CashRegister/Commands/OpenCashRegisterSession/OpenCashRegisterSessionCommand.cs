using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.OpenCashRegisterSession;

public sealed record OpenCashRegisterSessionCommand(Guid CashRegisterId, Guid OpenedBy, decimal OpeningBalance) : ICommand<Result<Guid>>;
