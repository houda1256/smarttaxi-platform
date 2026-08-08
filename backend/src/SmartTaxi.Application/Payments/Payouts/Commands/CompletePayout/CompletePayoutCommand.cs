using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Payouts.Commands.CompletePayout;

public sealed record CompletePayoutCommand(Guid RequestingUserId, Guid PayoutId) : ICommand<Result>;
