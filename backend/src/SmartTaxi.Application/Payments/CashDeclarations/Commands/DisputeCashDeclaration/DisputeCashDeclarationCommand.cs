using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.DisputeCashDeclaration;

public sealed record DisputeCashDeclarationCommand(Guid ReviewedBy, Guid CashDeclarationId) : ICommand<Result>;
