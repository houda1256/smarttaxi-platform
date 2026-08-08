using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Payouts.Commands.CancelPayout;

public sealed record CancelPayoutCommand(Guid RequestingUserId, Guid PayoutId) : ICommand<Result>;
