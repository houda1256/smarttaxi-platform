using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.Disputes.Commands.RejectFinancialDispute;

public sealed record RejectFinancialDisputeCommand(Guid ResolvedBy, Guid DisputeId, string Resolution) : ICommand<Result>;
