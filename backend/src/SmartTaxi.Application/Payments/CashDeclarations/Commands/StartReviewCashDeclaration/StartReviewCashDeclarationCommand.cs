using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.StartReviewCashDeclaration;

public sealed record StartReviewCashDeclarationCommand(Guid ReviewedBy, Guid CashDeclarationId) : ICommand<Result>;
