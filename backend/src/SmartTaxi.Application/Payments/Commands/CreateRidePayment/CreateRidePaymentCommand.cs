using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Application.Payments.Commands.CreateRidePayment;

public sealed record CreateRidePaymentCommand(Guid RequestingUserId, Guid RideId, PaymentMethod PaymentMethod) : ICommand<Result<Guid>>;
