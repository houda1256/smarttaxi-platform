using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.SettleCashDeclaration;

public sealed record SettleCashDeclarationCommand(Guid SettledBy, Guid CashDeclarationId) : ICommand<Result>;
