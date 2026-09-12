using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Commands.CancelPayment;

public sealed record CancelPaymentCommand(Guid RequestingUserId, Guid PaymentId, string? Reason) : ICommand<Result>;
