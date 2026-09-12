using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;

namespace SmartTaxi.Application.Payments.Disputes.Commands.ResolveFinancialDispute;

public sealed record ResolveFinancialDisputeCommand(
    Guid ResolvedBy, Guid DisputeId, string Resolution, DisputeResolutionOutcome Outcome) : ICommand<Result>;
