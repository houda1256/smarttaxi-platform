using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Payouts.Commands.RejectPayout;

public sealed record RejectPayoutCommand(Guid RejectedBy, Guid PayoutId) : ICommand<Result>;
