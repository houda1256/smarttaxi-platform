using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.RegisterCashRegister;

public sealed record RegisterCashRegisterCommand(Guid OwnerId, string Label) : ICommand<Result<Guid>>;
