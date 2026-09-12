using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Commands.AuthorizePayment;

public sealed record AuthorizePaymentCommand(Guid RequestingUserId, Guid PaymentId) : ICommand<Result>;
