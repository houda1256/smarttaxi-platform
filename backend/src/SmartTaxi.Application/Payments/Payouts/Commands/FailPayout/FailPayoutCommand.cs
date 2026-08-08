using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Payouts.Commands.FailPayout;

public sealed record FailPayoutCommand(Guid RequestingUserId, Guid PayoutId, string Reason) : ICommand<Result>;
