using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Payouts.Commands.ApprovePayout;

public sealed record ApprovePayoutCommand(Guid ApprovedBy, Guid PayoutId) : ICommand<Result>;
