using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Payouts.Commands.StartProcessingPayout;

public sealed record StartProcessingPayoutCommand(Guid RequestingUserId, Guid PayoutId) : ICommand<Result>;
