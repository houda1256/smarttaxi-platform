using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Commands.ConfirmPayment;

public sealed record ConfirmPaymentCommand(Guid RequestingUserId, Guid PaymentId) : ICommand<Result<decimal>>;
