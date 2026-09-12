using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.ApproveCashDeclaration;

public sealed record ApproveCashDeclarationCommand(Guid ReviewedBy, Guid CashDeclarationId) : ICommand<Result>;
